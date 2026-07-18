// ============================================================================
// WebP Migration Tool — converts legacy PNG/JPG product images to WebP
// (local dev stores PNGs under wwwroot/uploads; production stores JPGs
//  under wwwroot/products — both are scanned when present)
// Run with: dotnet run tools/WebpMigration.cs -- <mode> [--batch-size N]
//
// Modes:
//   dry-run    (default) report what would be converted/updated; changes nothing
//   convert    convert PNG files to sibling .webp files (originals are KEPT)
//   update-db  rewrite DB image paths .png -> .webp (only where .webp exists on
//              disk); writes a CSV backup of old/new values BEFORE any update
//
// Safety properties:
//   - Never deletes or overwrites a PNG. Never overwrites an existing .webp.
//   - Idempotent: re-running skips files already converted / rows already updated.
//   - Per-file errors are logged and skipped; the run continues.
//   - DB rollback: apply the CSV backup (Table,Id,Column,OldValue) to restore.
// ============================================================================
#:package SixLabors.ImageSharp@3.1.*
#:package Npgsql@8.0.*

using System.Text;
using Npgsql;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

var repoRoot = FindRepoRoot();
var wwwroot = Path.Combine(repoRoot, "Bolcko.Web.App", "wwwroot");
var imageDirs = new[] { "uploads", "products" }
    .Select(d => Path.Combine(wwwroot, d))
    .Where(Directory.Exists)
    .ToList();
var legacyExtensions = new[] { ".png", ".jpg", ".jpeg" };
var backupDir = Path.Combine(repoRoot, "tools", "webp-migration-backups");

var mode = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "dry-run";
var batchSize = 100;
for (int i = 0; i < args.Length - 1; i++)
    if (args[i] == "--batch-size" && int.TryParse(args[i + 1], out var b)) batchSize = b;

if (mode is not ("dry-run" or "convert" or "update-db"))
{
    Console.Error.WriteLine($"Unknown mode '{mode}'. Use: dry-run | convert | update-db");
    return 1;
}

Console.WriteLine($"Mode: {mode} | scanning: {string.Join(", ", imageDirs)}");
if (imageDirs.Count == 0) { Console.Error.WriteLine("No image directories (uploads/products) found under wwwroot."); return 1; }

// ----------------------------------------------------------------------------
// Phase 1: file conversion (dry-run / convert)
// ----------------------------------------------------------------------------
if (mode is "dry-run" or "convert")
{
    var legacyFiles = imageDirs
        .SelectMany(d => Directory.EnumerateFiles(d, "*.*", SearchOption.AllDirectories))
        .Where(f => legacyExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
        .ToList();
    var pending = legacyFiles.Where(p => !File.Exists(Path.ChangeExtension(p, ".webp"))).ToList();
    Console.WriteLine($"Legacy image files (png/jpg/jpeg): {legacyFiles.Count} total, {legacyFiles.Count - pending.Count} already converted, {pending.Count} pending.");

    if (mode == "convert")
    {
        var encoder = new WebpEncoder { Quality = 75, Method = WebpEncodingMethod.Level4 }; // matches ImageService
        int done = 0, failed = 0;
        long savedBytes = 0;
        var failures = new List<string>();

        foreach (var chunk in pending.Chunk(batchSize))
        {
            foreach (var png in chunk)
            {
                var webp = Path.ChangeExtension(png, ".webp");
                try
                {
                    using var image = await Image.LoadAsync(png);
                    image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(1200, 1200) }));
                    await image.SaveAsync(webp, encoder);
                    savedBytes += new FileInfo(png).Length - new FileInfo(webp).Length;
                    done++;
                }
                catch (Exception ex)
                {
                    failed++;
                    failures.Add($"{png} :: {ex.Message}");
                    if (File.Exists(webp)) File.Delete(webp); // don't leave partial output
                }
            }
            Console.WriteLine($"  progress: {done + failed}/{pending.Count} (ok={done}, failed={failed}, saved={savedBytes / 1024 / 1024} MB)");
        }

        Console.WriteLine($"Conversion finished: ok={done}, failed={failed}, disk saved ≈ {savedBytes / 1024 / 1024} MB (originals kept).");
        if (failures.Count > 0)
        {
            Directory.CreateDirectory(backupDir);
            var failLog = Path.Combine(backupDir, $"convert-failures-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            File.WriteAllLines(failLog, failures);
            Console.WriteLine($"Failures logged to {failLog}");
        }
    }
}

// ----------------------------------------------------------------------------
// Phase 2: DB path update (dry-run / update-db)
// ----------------------------------------------------------------------------
if (mode is "dry-run" or "update-db")
{
    var connString = ReadConnectionString(repoRoot);
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // (table, idColumn, valueColumn) pairs that hold image paths
    var targets = new (string Table, string IdCol, string ValCol)[]
    {
        ("ProductImages", "Id", "Url"),
        ("Products", "Id", "ImageUrl"),
    };

    var changes = new List<(string Table, string Id, string Col, string Old, string New)>();

    foreach (var t in targets)
    {
        await using var cmd = new NpgsqlCommand(
            $"SELECT \"{t.IdCol}\"::text, \"{t.ValCol}\" FROM \"{t.Table}\" WHERE \"{t.ValCol}\" ~* '\\.(png|jpg|jpeg)'", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetString(0);
            var oldVal = reader.IsDBNull(1) ? null : reader.GetString(1);
            if (string.IsNullOrWhiteSpace(oldVal)) continue;

            // Products.ImageUrl can be a comma-separated list; handle per segment.
            var segments = oldVal.Split(',', StringSplitOptions.TrimEntries);
            var newSegments = segments.Select(s => MapSegment(s, wwwroot)).ToArray();
            var newVal = string.Join(",", newSegments);
            if (newVal != oldVal)
                changes.Add((t.Table, id, t.ValCol, oldVal, newVal));
        }
    }

    Console.WriteLine($"DB rows needing update (webp exists on disk): {changes.Count}");
    foreach (var group in changes.GroupBy(c => c.Table))
        Console.WriteLine($"  {group.Key}: {group.Count()} rows");

    if (mode == "update-db" && changes.Count > 0)
    {
        // CSV backup BEFORE any update
        Directory.CreateDirectory(backupDir);
        var csvPath = Path.Combine(backupDir, $"db-backup-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        var sb = new StringBuilder("Table,Id,Column,OldValue,NewValue\n");
        foreach (var c in changes)
            sb.AppendLine($"{c.Table},{c.Id},{c.Col},\"{c.Old.Replace("\"", "\"\"")}\",\"{c.New.Replace("\"", "\"\"")}\"");
        File.WriteAllText(csvPath, sb.ToString());
        Console.WriteLine($"Backup written: {csvPath}");

        int updated = 0, dbFailed = 0;
        foreach (var chunk in changes.Chunk(batchSize))
        {
            await using var tx = await conn.BeginTransactionAsync();
            foreach (var c in chunk)
            {
                try
                {
                    await using var up = new NpgsqlCommand(
                        $"UPDATE \"{c.Table}\" SET \"{c.Col}\" = @new WHERE \"Id\"::text = @id AND \"{c.Col}\" = @old", conn, tx);
                    up.Parameters.AddWithValue("new", c.New);
                    up.Parameters.AddWithValue("old", c.Old);
                    up.Parameters.AddWithValue("id", c.Id);
                    updated += await up.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    dbFailed++;
                    Console.Error.WriteLine($"  FAILED {c.Table}#{c.Id}: {ex.Message}");
                }
            }
            await tx.CommitAsync();
            Console.WriteLine($"  progress: {updated + dbFailed}/{changes.Count} rows (ok={updated}, failed={dbFailed})");
        }
        Console.WriteLine($"DB update finished: updated={updated}, failed={dbFailed}. Rollback data in {csvPath}");
    }
}

return 0;

// ----------------------------------------------------------------------------
static string MapSegment(string segment, string wwwroot)
{
    // Only rewrite local .png/.jpg/.jpeg paths whose .webp twin actually exists on disk.
    if (string.IsNullOrWhiteSpace(segment)) return segment;
    if (segment.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return segment;
    var ext = Path.GetExtension(segment).ToLowerInvariant();
    if (ext is not (".png" or ".jpg" or ".jpeg")) return segment;

    var relative = segment.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
    var legacyPath = Path.Combine(wwwroot, relative);
    var webpPath = Path.ChangeExtension(legacyPath, ".webp");
    if (!File.Exists(webpPath)) return segment; // not converted yet -> leave untouched

    return segment[..^ext.Length] + ".webp";
}

static string ReadConnectionString(string repoRoot)
{
    var appsettings = Path.Combine(repoRoot, "Bolcko.Web.App", "appsettings.json");
    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(appsettings));
    return doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()
        ?? throw new InvalidOperationException("DefaultConnection not found in appsettings.json");
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Bolcko.Web.App", "Bolcko.Web.App.csproj")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Run this tool from inside the Bolcko.Web repository.");
}
