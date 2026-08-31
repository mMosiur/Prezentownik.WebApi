using Prezentownik.WebApi.Modules.Auth.Email.Templates;
using Prezentownik.WebApi.Services.Email;
using Prezentownik.WebApi.Services.Html;

namespace Prezentownik.WebApi.Modules.Auth.Email;

public sealed class IdentityEmailTemplateService(IRazorHtmlRenderer renderer)
    : IIdentityEmailTemplateService
{
    private readonly IRazorHtmlRenderer _renderer = renderer;

    public async Task<EmailBody> RenderEmailConfirmationAsync(string confirmationLink)
    {
        var html = await _renderer.RenderAsync<EmailConfirmationTemplate>(new()
        {
            [nameof(EmailConfirmationTemplate.ConfirmationLink)] = confirmationLink
        });

        var plainText =
            $"""
             Witaj w serwisie Prezentownik!

             Dziękujemy za rejestrację. Aby aktywować swoje konto, przejdź pod poniższy adres:
             {confirmationLink}

             Jeśli to nie Ty zakładałeś konto, zignoruj tę wiadomość.
             """;

        return new(html, plainText);
    }

    public async Task<EmailBody> RenderEmailPasswordResetLinkAsync(string resetLink)
    {
        var html = await _renderer.RenderAsync<PasswordResetLinkTemplate>(new()
        {
            [nameof(PasswordResetLinkTemplate.ResetLink)] = resetLink
        });

        var plainText =
            $"""
             Resetowanie hasła w serwisie Prezentownik.

             Aby ustawić nowe hasło, przejdź pod poniższy adres:
             {resetLink}

             Jeśli nie prosiłeś o reset hasła, zignoruj tę wiadomość.
             """;

        return new(html, plainText);
    }
}
