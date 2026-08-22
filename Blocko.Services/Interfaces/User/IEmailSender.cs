using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.User
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendEmailAsync(string email, string subject, string htmlMessage, IEnumerable<(byte[] content, string fileName, string contentType)> attachments);
    }
}
