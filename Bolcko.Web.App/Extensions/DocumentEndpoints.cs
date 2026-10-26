using System.Security.Claims;
using Bolcko.Web.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Extensions;

/// <summary>
/// Serves the private documents (KYC, weighbridge tickets...) that are stored in S3.
/// They were public files on the old server; now only logged-in users can open them:
/// admins can open everything, other users only their own files.
/// When S3 is not configured (local development) the normal static-files middleware serves them as before.
/// </summary>
public static class DocumentEndpoints
{
    private static readonly string[] AdminRoles = { "SuperAdmin", "Admin", "DashboardUser" };
    private static readonly string[] WeighbridgeRoles = { "Vendor", "Supplier", "DeliveryDriver", "DeliveryCompanyUser" };

    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapGet("/uploads/{**path}", async (string path, HttpContext ctx, [FromServices] IDocumentStorage storage) =>
        {
            if (!storage.UseS3) return Results.NotFound();

            var relative = ("uploads/" + path).Replace("\\", "/");
            if (relative.Contains("..")) return Results.NotFound();

            if (!IsAllowed(ctx.User, relative)) return Results.Forbid();

            var doc = await storage.OpenAsync(relative);
            if (doc == null) return Results.NotFound();

            ctx.Response.Headers["Cache-Control"] = "private, no-store";
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            if (doc.ContentType == "application/octet-stream")
                ctx.Response.Headers["Content-Disposition"] = "attachment";

            return Results.Stream(doc.Stream, doc.ContentType);
        }).RequireAuthorization();
    }

    private static bool IsAllowed(ClaimsPrincipal user, string relative)
    {
        if (AdminRoles.Any(user.IsInRole)) return true;

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries); // uploads/<folder>/...

        if (parts.Length >= 3 && parts[1] == "weighbridge")
            return WeighbridgeRoles.Any(user.IsInRole);

        if (string.IsNullOrEmpty(userId)) return false;

        // uploads/delivery/<userId>/file, uploads/contractors/<userId>/file, uploads/investors/<userId>/file
        if (parts.Length >= 4 && parts[2] == userId) return true;

        // uploads/vendor-kyc/<docType>_<userId>_<guid>.ext
        if (parts.Length == 3 && parts[1] == "vendor-kyc" && parts[2].Contains("_" + userId + "_")) return true;

        return false;
    }
}
