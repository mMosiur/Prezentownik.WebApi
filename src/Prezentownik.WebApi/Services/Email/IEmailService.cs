namespace Prezentownik.WebApi.Services.Email;

public interface IEmailService
{
    Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        string plainTextBody,
        CancellationToken cancellationToken);
}
