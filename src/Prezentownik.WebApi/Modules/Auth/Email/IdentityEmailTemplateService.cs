using Prezentownik.WebApi.Services.Email;

namespace Prezentownik.WebApi.Modules.Auth.Email;

public sealed class IdentityEmailTemplateService
    : IIdentityEmailTemplateService
{
    public EmailBody RenderEmailConfirmation(string confirmationLink)
    {
        var html = EmailTemplate.RenderEmailHtml(
            title: "Potwierdź swój adres e-mail",
            heading: "Witaj w serwisie Prezentownik!",
            buttonText: "Potwierdź adres e-mail",
            buttonUrl: confirmationLink,
            footerNote: "Jeśli to nie Ty zakładałeś konto w serwisie Prezentownik, możesz zignorować tę wiadomość.",
            childContent:
            //language=html
            """
            <p style="margin: 0 0 16px; font-size: 16px; line-height: 24px; color: #374151;">
                Dziękujemy za rejestrację. Aby aktywować swoje konto i zacząć tworzyć listy prezentów, potwierdź swój adres e-mail.
            </p>
            """
        );

        var plainText =
            $"""
             Witaj w serwisie Prezentownik!

             Dziękujemy za rejestrację. Aby aktywować swoje konto, przejdź pod poniższy adres:
             {confirmationLink}

             Jeśli to nie Ty zakładałeś konto, zignoruj tę wiadomość.
             """;

        return new(html, plainText);
    }

    public EmailBody RenderEmailPasswordResetLink(string resetLink)
    {
        var html = EmailTemplate.RenderEmailHtml(
            title: "Zresetuj swoje hasło",
            heading: "Resetowanie hasła",
            buttonText: "Zresetuj hasło",
            buttonUrl: resetLink,
            footerNote: "Link jest ważny przez ograniczony czas. Jeśli nie prosiłeś o reset hasła, Twoje konto jest bezpieczne – zignoruj tę wiadomość.",
            childContent:
            //language=html
            """
            <p style="margin: 0 0 16px; font-size: 16px; line-height: 24px; color: #374151;">
                Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta w serwisie Prezentownik. Kliknij poniższy przycisk, aby ustawić nowe hasło.
            </p>
            """
        );

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
