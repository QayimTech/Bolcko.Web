using Blocko.Services.Interfaces.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Net.Mail;
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
            _logger.LogInformation("Sending email to {Email} with subject {Subject} and {Count} attachments", email, subject, attachments.Count());

            // Attempt to read SMTP settings from configuration if available
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPortStr = _configuration["Smtp:Port"];
            var smtpUser = _configuration["Smtp:Username"];
            var smtpPass = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? "no-reply@bolcko.com";

            if (!string.IsNullOrEmpty(smtpHost) && int.TryParse(smtpPortStr, out int smtpPort))
            {
                try
                {
                    using (var client = new SmtpClient(smtpHost, smtpPort))
                    {
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                        client.EnableSsl = true;

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(fromEmail),
                            Subject = subject,
                            Body = htmlMessage,
                            IsBodyHtml = true
                        };
                        mailMessage.To.Add(email);

                        foreach (var att in attachments)
                        {
                            var attachment = new Attachment(new MemoryStream(att.content), att.fileName, att.contentType);
                            mailMessage.Attachments.Add(attachment);
                        }

                        await client.SendMailAsync(mailMessage);
                        _logger.LogInformation("Email sent successfully via SMTP to {Email}", email);
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email via SMTP. Falling back to local file saving.");
                }
            }

            // Fallback: Save email to local wwwroot folder for development/testing
            try
            {
                var basePath = Directory.GetCurrentDirectory();
                var directoryPath = Path.Combine(basePath, "wwwroot", "sent-emails");
                if (!Directory.Exists(Path.Combine(basePath, "wwwroot")) && Directory.Exists(Path.Combine(basePath, "Bolcko.Web.App")))
                {
                    directoryPath = Path.Combine(basePath, "Bolcko.Web.App", "wwwroot", "sent-emails");
                }

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var baseName = $"{System.DateTime.UtcNow:yyyyMMdd_HHmmss}_{email.Replace("@", "_").Replace(".", "_")}";
                var filePath = Path.Combine(directoryPath, $"{baseName}.html");
                
                await File.WriteAllTextAsync(filePath, htmlMessage);
                _logger.LogInformation("Email saved locally to {FilePath} because SMTP is not configured.", filePath);

                // Save attachments locally as well for inspection
                foreach (var att in attachments)
                {
                    var attPath = Path.Combine(directoryPath, $"{baseName}_{att.fileName}");
                    await File.WriteAllBytesAsync(attPath, att.content);
                    _logger.LogInformation("Email attachment saved locally to {FilePath}", attPath);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to save email locally.");
            }
        }
    }
}
