using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Prezentownik.WebApi.Models;
using Serilog;

namespace Prezentownik.WebApi.Modules.Auth.Email;

public sealed class LoggingOnlyConfirmationEmailSender : IEmailSender<AppUser>
{
    private readonly string _senderAddress;
    private readonly string _frontendBaseUrl;

    public LoggingOnlyConfirmationEmailSender(IConfiguration configuration)
    {
        _senderAddress = configuration["AzureCommunicationServices:SenderAddress"]
            ?? "donotreply@prezentownik.info.pl";
        _frontendBaseUrl = configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl is not configured.");
    }

    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        var link = ToFrontendLink("confirm-email", confirmationLink);
        LogMessage(
            email,
            "Potwierdź swój adres e-mail",
            $"Potwierdź swoje konto w serwisie Prezentownik, przechodząc pod adres: {link}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var link = ToFrontendLink("reset-password", resetLink);
        LogMessage(
            email,
            "Zresetuj swoje hasło",
            $"Możesz zresetować swoje hasło w serwisie Prezentownik, przechodząc pod adres: {link}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        LogMessage(
            email,
            "Zresetuj swoje hasło",
            $"Twój kod do zresetowania hasła w serwisie Prezentownik to: {resetCode}");
        return Task.CompletedTask;
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

    private void LogMessage(string email, string subject, string plainTextBody)
    {
        Log.Information("Sending email from {SenderEmail} to {RecipientEmail}, subject: {Subject}, plain-text body: {PlainTextBody}",
            _senderAddress,
            email,
            subject,
            plainTextBody);
    }
}
