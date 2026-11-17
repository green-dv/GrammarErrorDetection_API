using GrammarErrorDetection.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class EmailRepository : IEmailRepository
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<EmailRepository>? logger;
        public EmailRepository(IConfiguration configuration, ILogger<EmailRepository>? logger = null)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task SendVerificationEmailAsync(string name, string email, string message, string subject)
        {
            string senderEmail = configuration["GMAIL_EMAIL"] ?? configuration["Gmail:Email"] ?? throw new InvalidOperationException("GMAIL_EMAIL no configurado.");
            string senderPassword = configuration["GMAIL_APPPASSWORD"] ?? configuration["Gmail:AppPassword"] ?? throw new InvalidOperationException("GMAIL_APPPASSWORD no configurado.");

            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "Grammar Error Detection"),
                Subject = subject,
                Body = $"Hola {name}, {message}",
                IsBodyHtml = false,
            };

            mailMessage.To.Add(new MailAddress(email, name));

            // Se registra solo el asunto: nunca el destinatario ni el cuerpo (traen códigos y contraseñas temporales).
            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                logger?.LogInformation("Correo enviado: {Subject}", subject);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, "No se pudo enviar el correo: {Subject}", subject);
                throw;
            }
        }
    }
}
