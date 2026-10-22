using Amazon.S3;
using Amazon.S3.Model;
using Blocko.Services;
using Microsoft.Extensions.Options;

namespace Bolcko.Web.App.Services;

/// <summary>A document read back from storage (the caller disposes the stream).</summary>
public class DocumentContent
{
    public Stream Stream { get; set; } = Stream.Null;
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// Stores private documents (KYC, contractor/investor papers, weighbridge tickets).
/// - S3 configured (S3__BucketName)  -> uploads to S3 (KMS encrypted), key = "uploads/<folder>/<file>".
/// - Otherwise                         -> saves under wwwroot/uploads on local disk (local development).
/// The value returned to callers is the same "/uploads/<folder>/<file>" path that was stored in the DB before.
/// </summary>
public interface IDocumentStorage
{
    bool UseS3 { get; }

    /// <summary>Saves the file and returns its public-style path, e.g. "/uploads/vendor-kyc/abc.pdf".</summary>
    Task<string> SaveAsync(IFormFile file, string folder, string fileName);

    /// <summary>Same as SaveAsync, but for public files (e.g. notification sounds). The key is "&lt;folder&gt;/&lt;file&gt;" without the "uploads/" prefix.</summary>
    Task<string> SavePublicAsync(IFormFile file, string folder, string fileName);

    /// <summary>Reads a stored document from S3. Returns null when it does not exist.</summary>
    Task<DocumentContent?> OpenAsync(string relativePath);
}

public class DocumentStorage : IDocumentStorage
{
    private readonly IAmazonS3? _s3;
    private readonly string _bucket;
    private readonly string _prefix;
    private readonly string _kmsKeyId;
    private readonly string _webRoot;

    public DocumentStorage(IOptions<ImageSettings> settings, IWebHostEnvironment env, IAmazonS3? s3 = null)
    {
        _s3 = s3;
        _bucket = settings.Value.S3BucketName?.Trim() ?? string.Empty;
        _kmsKeyId = settings.Value.S3KmsKeyId?.Trim() ?? string.Empty;

        var prefix = (settings.Value.S3KeyPrefix ?? string.Empty).Replace("\\", "/").Trim().Trim('/');
        _prefix = prefix.Length == 0 ? string.Empty : prefix + "/";

        _webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    }

    public bool UseS3 => _s3 != null && !string.IsNullOrWhiteSpace(_bucket);

    public Task<string> SaveAsync(IFormFile file, string folder, string fileName)
        => StoreAsync(file, folder, fileName, true);

    public Task<string> SavePublicAsync(IFormFile file, string folder, string fileName)
        => StoreAsync(file, folder, fileName, false);

    private async Task<string> StoreAsync(IFormFile file, string folder, string fileName, bool underUploads)
    {
        var safeFolder = string.Join("/",
            folder.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Sanitize));
        var safeName = Sanitize(Path.GetFileName(fileName));
        var relative = underUploads ? $"uploads/{safeFolder}/{safeName}" : $"{safeFolder}/{safeName}";

        if (UseS3)
        {
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);
            buffer.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = _prefix + relative,
                InputStream = buffer,
                ContentType = GuessContentType(safeName),
                AutoCloseStream = false,
                // The bucket policy DENIES uploads that do not request KMS encryption.
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
            };
            if (!string.IsNullOrWhiteSpace(_kmsKeyId))
                request.ServerSideEncryptionKeyManagementServiceKeyId = _kmsKeyId;

            await _s3!.PutObjectAsync(request);
        }
        else
        {
            var fullPath = Path.Combine(_webRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            using var target = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(target);
        }

        return "/" + relative;
    }

    public async Task<DocumentContent?> OpenAsync(string relativePath)
    {
        if (!UseS3) return null;

        var key = _prefix + relativePath.Replace("\\", "/").TrimStart('/');
        try
        {
            var response = await _s3!.GetObjectAsync(_bucket, key);
            return new DocumentContent
            {
                Stream = response.ResponseStream,
                ContentType = GuessContentType(relativePath)
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound
                                           || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
    }

    /// <summary>Keeps only letters, digits, dot, dash and underscore so the S3 key is always safe.</summary>
    private static string Sanitize(string value)
    {
        var chars = value.Select(c => char.IsAsciiLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '_').ToArray();
        var cleaned = new string(chars).Trim('.');
        return cleaned.Length == 0 ? "file" : cleaned;
    }

    public static string GuessContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }
}
