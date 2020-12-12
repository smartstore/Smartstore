using System.Threading.Tasks;
using MailKit.Net.Smtp;

namespace Smartstore.Net.Mail
{
    /// <summary>
    /// Contract for mail sender
    /// </summary>
    public interface IMailSender
    {
        void SendMail(SmtpClient client, EmailMessage message);
        Task SendMailAsync(SmtpClient client, EmailMessage message);
    }
}
