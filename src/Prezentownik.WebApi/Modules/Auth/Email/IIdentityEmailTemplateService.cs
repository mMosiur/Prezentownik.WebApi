using Prezentownik.WebApi.Services.Email;

namespace Prezentownik.WebApi.Modules.Auth.Email;

public interface IIdentityEmailTemplateService
{
    EmailBody RenderEmailConfirmation(string confirmationLink);
    EmailBody RenderEmailPasswordResetLink(string resetLink);
}
