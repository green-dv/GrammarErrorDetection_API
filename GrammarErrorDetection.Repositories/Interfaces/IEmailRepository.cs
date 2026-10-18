namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IEmailRepository
    {
        Task SendVerificationEmailAsync(string name, string email, string message, string subject);
    }
}
