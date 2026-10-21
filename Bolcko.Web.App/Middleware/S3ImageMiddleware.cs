using Amazon.S3;
using Amazon.S3.Model;
using Blocko.Services;
using Microsoft.Extensions.Options;

namespace Bolcko.Web.App.Middleware
{
    /// <summary>
    /// Serves product/variant images from S3 when they are not found on local disk.
    /// Runs AFTER UseStaticFiles, so local files (e.g. logo, placeholders) are still served normally,
    /// and only requests for uploaded-image folders that static files could not find come here.
    /// Does nothing when S3 is not configured.
    /// </summary>
    public class S3ImageMiddleware
    {
        private static readonly string[] ImagePrefixes =
        {
            "/images/products/", "/images/variants/", "/products/", "/variants/", "/sounds/"
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<S3ImageMiddleware> _logger;

        public S3ImageMiddleware(RequestDelegate next, ILogger<S3ImageMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IOptions<ImageSettings> options, IServiceProvider services)
        {
            var settings = options.Value;
            var path = context.Request.Path.Value;

            if (!settings.UseS3
                || string.IsNullOrEmpty(path)
                || !(HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                || path.Contains("..")
                || !Path.HasExtension(path)   // page URLs like /products/xyz have no file extension
                || !ImagePrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            var s3 = services.GetService<IAmazonS3>();
            if (s3 == null)
            {
                await _next(context);
                return;
            }

            var prefix = string.IsNullOrWhiteSpace(settings.S3KeyPrefix)
                ? string.Empty
                : settings.S3KeyPrefix.Trim().Trim('/') + "/";
            var key = prefix + Uri.UnescapeDataString(path).TrimStart('/');

            try
            {
                using var response = await s3.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = settings.S3BucketName,
                    Key = key
                }, context.RequestAborted);

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = string.IsNullOrEmpty(response.Headers.ContentType)
                    ? Bolcko.Web.App.Services.DocumentStorage.GuessContentType(path)
                    : response.Headers.ContentType;
                context.Response.ContentLength = response.ContentLength;
                context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
                if (!string.IsNullOrEmpty(response.ETag))
                    context.Response.Headers["ETag"] = response.ETag;

                if (!HttpMethods.IsHead(context.Request.Method))
                    await response.ResponseStream.CopyToAsync(context.Response.Body, context.RequestAborted);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound
                                               || ex.ErrorCode == "NoSuchKey")
            {
                // Not in S3 either -> normal 404 behaviour of the app.
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                // Browser closed the connection; nothing to do.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read image from S3 for path {Path}", path);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;
                }
            }
        }
    }
}
