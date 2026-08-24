using Prezentownik.WebApi.Services.Email;

namespace Prezentownik.WebApi.Modules.Auth.Email;

public interface IIdentityEmailTemplateService
{
    Task<EmailBody> RenderEmailConfirmationAsync(string confirmationLink);
    Task<EmailBody> RenderEmailPasswordResetLinkAsync(string resetLink);
    Task<EmailBody> RenderEmailPasswordResetCodeAsync(string resetCode);
}
