using Blocko.Services.Interfaces.User;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.IO;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.user
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IConfiguration _configuration;

        public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailAsync(email, subject, htmlMessage, System.Linq.Enumerable.Empty<(byte[] content, string fileName, string contentType)>());
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage, IEnumerable<(byte[] content, string fileName, string contentType)> attachments)
        {
            _logger.LogInformation("Sending email to {Email} with subject {Subject}", email, subject);

            // Read config — supports both "Zoho:*" (JSON) and "Zoho__*" (env vars)
            var zohoClientId     = _configuration["Zoho:ClientId"]     ?? _configuration["Zoho__ClientId"]     ?? string.Empty;
            var zohoClientSecret = _configuration["Zoho:ClientSecret"] ?? _configuration["Zoho__ClientSecret"] ?? string.Empty;
            var zohoRefreshToken = _configuration["Zoho:RefreshToken"] ?? _configuration["Zoho__RefreshToken"] ?? string.Empty;
            var zohoAccountId    = _configuration["Zoho:AccountId"]    ?? _configuration["Zoho__AccountId"]    ?? "4042225000000008002";
            var fromEmail        = _configuration["Zoho:FromEmail"]    ?? _configuration["Zoho__FromEmail"]    ?? "noreply@block-o.com";
            var fromName         = _configuration["Zoho:FromName"]     ?? _configuration["Zoho__FromName"]     ?? "BLOCKO";

            // If Zoho OAuth credentials are configured, use Zoho Mail HTTP API (port 443 — never blocked)
            if (!string.IsNullOrWhiteSpace(zohoClientId) && !string.IsNullOrWhiteSpace(zohoRefreshToken))
            {
                _logger.LogInformation("Using Zoho Mail HTTP API to send email to {Email}...", email);
                await SendViaZohoApiAsync(email, subject, htmlMessage, zohoClientId, zohoClientSecret, zohoRefreshToken, zohoAccountId, fromEmail, fromName, attachments);
                return;
            }

            // Fallback: Read SMTP settings
            var smtpHost    = _configuration["Smtp:Host"]      ?? _configuration["Smtp__Host"]      ?? "smtp.zoho.com";
            var smtpPortStr = _configuration["Smtp:Port"]      ?? _configuration["Smtp__Port"]      ?? "465";
            var smtpUser    = _configuration["Smtp:Username"]  ?? _configuration["Smtp__Username"]  ?? string.Empty;
            var smtpPass    = _configuration["Smtp:Password"]  ?? _configuration["Smtp__Password"]  ?? string.Empty;

            if (string.IsNullOrWhiteSpace(smtpPass))
            {
                _logger.LogWarning("No email credentials configured. Saving email locally.");
                await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
                return;
            }

            // SendGrid detection
            if (smtpPass.StartsWith("SG.") || smtpHost.Contains("sendgrid"))
            {
                _logger.LogInformation("SendGrid API Key detected. Sending via SendGrid HTTP API...");
                await SendViaSendGridAsync(email, subject, htmlMessage, smtpPass, fromEmail, fromName, attachments);
                return;
            }

            // SMTP fallback (MailKit)
            if (!int.TryParse(smtpPortStr, out int smtpPort)) smtpPort = 465;
            var sslOption = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(MailboxAddress.Parse(email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
                foreach (var att in attachments)
                    bodyBuilder.Attachments.Add(att.fileName, att.content, ContentType.Parse(att.contentType));
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 10000;
                _logger.LogInformation("Attempting SMTP to {Host}:{Port}...", smtpHost, smtpPort);
                await client.ConnectAsync(smtpHost, smtpPort, sslOption);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email sent successfully via SMTP to {Email}", email);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "SMTP failed for {Email}: {Message}", email, ex.Message);
                await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
            }
        }

        private async Task SendViaZohoApiAsync(string email, string subject, string htmlMessage,
            string clientId, string clientSecret, string refreshToken, string accountId,
            string fromEmail, string fromName,
            IEnumerable<(byte[] content, string fileName, string contentType)> attachments)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();

                // Step 1: Get a fresh Access Token using the Refresh Token
                var tokenBody = new System.Net.Http.FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("refresh_token", refreshToken),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_secret", clientSecret),
                    new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "refresh_token"),
                });

                var tokenResponse = await http.PostAsync("https://accounts.zoho.com/oauth/v2/token", tokenBody);
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Zoho token response: {Response}", tokenJson);

                var tokenDoc = System.Text.Json.JsonDocument.Parse(tokenJson);
                if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
                {
                    _logger.LogError("Failed to get Zoho access token. Response: {Response}", tokenJson);
                    await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
                    return;
                }
                var accessToken = accessTokenElement.GetString()!;

                // Step 2: Send email via Zoho Mail API
                var payload = new
                {
                    fromAddress = fromEmail,
                    toAddress = email,
                    subject = subject,
                    content = htmlMessage,
                    mailFormat = "html"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post,
                    $"https://mail.zoho.com/api/accounts/{accountId}/messages");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await http.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Email sent successfully via Zoho Mail API to {Email}", email);
                else
                {
                    _logger.LogError("Zoho Mail API failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                    await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Zoho Mail API exception for {Email}: {Message}", email, ex.Message);
                await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
            }
        }

        private async Task SendViaSendGridAsync(string email, string subject, string htmlMessage,
            string apiKey, string fromEmail, string fromName,
            IEnumerable<(byte[] content, string fileName, string contentType)> attachments)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    personalizations = new[] { new { to = new[] { new { email } } } },
                    from = new { email = fromEmail, name = fromName },
                    subject,
                    content = new[] { new { type = "text/html", value = htmlMessage } }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var httpContent = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await http.PostAsync("https://api.sendgrid.com/v3/mail/send", httpContent);

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Email sent successfully via SendGrid to {Email}", email);
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("SendGrid failed. Status: {Status}, Body: {Body}", response.StatusCode, error);
                    await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "SendGrid exception for {Email}", email);
                await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
            }
        }

        private async Task SaveEmailLocallyAsync(string email, string subject, string htmlMessage,
            IEnumerable<(byte[] content, string fileName, string contentType)> attachments)
        {
            try
            {
                var basePath = Directory.GetCurrentDirectory();
                var directoryPath = Path.Combine(basePath, "wwwroot", "sent-emails");
                if (!Directory.Exists(Path.Combine(basePath, "wwwroot")) && Directory.Exists(Path.Combine(basePath, "Bolcko.Web.App")))
                    directoryPath = Path.Combine(basePath, "Bolcko.Web.App", "wwwroot", "sent-emails");
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                var baseName = $"{System.DateTime.UtcNow:yyyyMMdd_HHmmss}_{email.Replace("@", "_").Replace(".", "_")}";
                await File.WriteAllTextAsync(Path.Combine(directoryPath, $"{baseName}.html"), htmlMessage);
                _logger.LogInformation("Email saved locally for {Email}", email);

                foreach (var att in attachments)
                    await File.WriteAllBytesAsync(Path.Combine(directoryPath, $"{baseName}_{att.fileName}"), att.content);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to save email locally.");
            }
        }
    }
}
