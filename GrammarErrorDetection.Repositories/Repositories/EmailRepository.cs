using GrammarErrorDetection.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace GrammarErrorDetection.Repositories.Repositories
{
    internal class EmailRepository : IEmailRepository
    {
        private readonly IConfiguration configuration;
        public EmailRepository(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendVerificationEmailAsync(string name, string email, string message, string subject)
        {
            string senderEmail = configuration["Gmail:Email"] ?? throw new Exception("Gmail Email not configured");
            string senderPassword = configuration["Gmail:AppPassword"] ?? throw new Exception("Gmail AppPassword not configured");

            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "Grammar Error Detection"),
                Subject = subject,
                Body = $"Hola {name}, {message}",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(new MailAddress(email, name));

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
