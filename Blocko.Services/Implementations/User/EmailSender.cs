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

            // Read SMTP settings — supports both "Smtp:Host" (JSON) and "Smtp__Host" (env vars)
            var smtpHost     = _configuration["Smtp:Host"]      ?? _configuration["Smtp__Host"]      ?? "smtp.zoho.com";
            var smtpPortStr  = _configuration["Smtp:Port"]      ?? _configuration["Smtp__Port"]      ?? "465";
            var smtpUser     = _configuration["Smtp:Username"]  ?? _configuration["Smtp__Username"]  ?? string.Empty;
            var smtpPass     = _configuration["Smtp:Password"]  ?? _configuration["Smtp__Password"]  ?? string.Empty;
            var fromEmail    = _configuration["Smtp:FromEmail"] ?? _configuration["Smtp__FromEmail"] ?? "noreply@block-o.com";
            var fromName     = _configuration["Smtp:FromName"]  ?? _configuration["Smtp__FromName"]  ?? "BLOCKO";

            _logger.LogInformation("SMTP Config — Host: {Host}, Port: {Port}, User: {User}, FromEmail: {From}",
                smtpHost, smtpPortStr, smtpUser, fromEmail);

            if (string.IsNullOrWhiteSpace(smtpPass))
            {
                _logger.LogWarning("SMTP password is not configured. Falling back to local file saving.");
                await SaveEmailLocallyAsync(email, subject, htmlMessage, attachments);
                return;
            }

            if (!int.TryParse(smtpPortStr, out int smtpPort))
                smtpPort = 465;

            // Choose SSL mode based on port
            // Port 465 = implicit SSL (SslOnConnect)
            // Port 587 = STARTTLS (StartTls)
            var secureSocketOptions = smtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(MailboxAddress.Parse(email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };

                foreach (var att in attachments)
                {
                    bodyBuilder.Attachments.Add(att.fileName, att.content, ContentType.Parse(att.contentType));
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 10000; // 10 second timeout — fail fast if SMTP is unreachable
                await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully via MailKit to {Email}", email);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via MailKit SMTP to {Email}.", email);
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

                if (!Directory.Exists(Path.Combine(basePath, "wwwroot")) &&
                    Directory.Exists(Path.Combine(basePath, "Bolcko.Web.App")))
                {
                    directoryPath = Path.Combine(basePath, "Bolcko.Web.App", "wwwroot", "sent-emails");
                }

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                var baseName = $"{System.DateTime.UtcNow:yyyyMMdd_HHmmss}_{email.Replace("@", "_").Replace(".", "_")}";
                var filePath = Path.Combine(directoryPath, $"{baseName}.html");

                await File.WriteAllTextAsync(filePath, htmlMessage);
                _logger.LogInformation("Email saved locally to {FilePath}", filePath);

                foreach (var att in attachments)
                {
                    var attPath = Path.Combine(directoryPath, $"{baseName}_{att.fileName}");
                    await File.WriteAllBytesAsync(attPath, att.content);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to save email locally.");
            }
        }
    }
}
