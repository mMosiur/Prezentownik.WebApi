using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Services.Email;

namespace Prezentownik.WebApi.Modules.Auth.Email;

public sealed class IdentityEmailSender : IEmailSender<AppUser>
{
    private readonly string _frontendBaseUrl;
    private readonly IEmailService _emailService;
    private readonly IIdentityEmailTemplateService _templateService;

    public IdentityEmailSender(IConfiguration configuration, IIdentityEmailTemplateService templateService, IEmailService emailService)
    {
        _frontendBaseUrl = configuration["Frontend:BaseUrl"]
                        ?? throw new InvalidOperationException("Frontend:BaseUrl is not configured.");
        _emailService = emailService;
        _templateService = templateService;
    }

    public async Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        var link = ToFrontendLink("confirm-email", confirmationLink);
        var emailBody = await _templateService.RenderEmailConfirmationAsync(link);
        await _emailService.SendAsync(
            recipient: email,
            subject: "Potwierdź swój adres e-mail",
            htmlBody: emailBody.Html,
            plainTextBody: emailBody.PlainText,
            CancellationToken.None);
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var link = ToFrontendLink("reset-password", resetLink);
        var emailBody = await _templateService.RenderEmailPasswordResetLinkAsync(link);
        await _emailService.SendAsync(
            recipient: email,
            subject: "Zresetuj swoje hasło",
            htmlBody: emailBody.Html,
            plainTextBody: emailBody.PlainText,
            CancellationToken.None);
    }

    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        var link = QueryHelpers.AddQueryString(
            $"{_frontendBaseUrl.AsSpan().TrimEnd('/')}/reset-password",
            new Dictionary<string, string?>
            {
                ["email"] = email,
                ["code"] = resetCode
            });
        var emailBody = await _templateService.RenderEmailPasswordResetLinkAsync(link);
        await _emailService.SendAsync(
            recipient: email,
            subject: "Zresetuj swoje hasło",
            htmlBody: emailBody.Html,
            plainTextBody: emailBody.PlainText,
            CancellationToken.None);
    }

    private string ToFrontendLink(string frontendPath, string apiLink)
    {
        // MapIdentityApi builds confirmationLink/resetLink as an absolute, HTML-encoded URL pointing back
        // at this API (e.g. "{api}/auth/confirmEmail?userId=...&code=..."). We only care about the query
        // parameters, so we forward those to the corresponding frontend page instead.
        var decodedLink = WebUtility.HtmlDecode(apiLink);
        var query = QueryHelpers.ParseQuery(new Uri(decodedLink).Query);

        var frontendUrl = $"{_frontendBaseUrl.TrimEnd('/')}/{frontendPath}";
        foreach (var (key, value) in query)
        {
            frontendUrl = QueryHelpers.AddQueryString(frontendUrl, key, value.ToString());
        }

        return frontendUrl;
    }
}
