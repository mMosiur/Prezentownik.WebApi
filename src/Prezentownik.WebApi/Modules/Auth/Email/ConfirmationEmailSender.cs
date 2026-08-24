using System.Net;
using Azure;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Prezentownik.WebApi.Models;
using Serilog;

namespace Prezentownik.WebApi.Modules.Auth.Email;

public sealed class ConfirmationEmailSender : IEmailSender<AppUser>
{
    private readonly EmailClient _emailClient;
    private readonly string _senderAddress;
    private readonly string _frontendBaseUrl;

    public ConfirmationEmailSender(IConfiguration configuration)
    {
        _senderAddress = configuration["AzureCommunicationServices:SenderAddress"]
            ?? throw new InvalidOperationException("AzureCommunicationServices:SenderAddress is not configured.");
        _frontendBaseUrl = configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl is not configured.");

        var connectionString = configuration["AzureCommunicationServices:ConnectionString"]
                            ?? throw new InvalidOperationException("AzureCommunicationServices:ConnectionString is not configured.");
        _emailClient = new(connectionString);
    }

    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        var link = ToFrontendLink("confirm-email", confirmationLink);
        return SendAsync(
            email,
            "Potwierdź swój adres e-mail",
            $"<p>Potwierdź swoje konto w serwisie Prezentownik, klikając poniższy link:</p><p><a href=\"{link}\">Potwierdź adres e-mail</a></p>",
            $"Potwierdź swoje konto w serwisie Prezentownik, przechodząc pod adres: {link}");
    }

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var link = ToFrontendLink("reset-password", resetLink);
        return SendAsync(
            email,
            "Zresetuj swoje hasło",
            $"<p>Możesz zresetować swoje hasło w serwisie Prezentownik, klikając poniższy link:</p><p><a href=\"{link}\">Zresetuj hasło</a></p>",
            $"Możesz zresetować swoje hasło w serwisie Prezentownik, przechodząc pod adres: {link}");
    }

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        return SendAsync(
            email,
            "Zresetuj swoje hasło",
            $"<p>Twój kod do zresetowania hasła w serwisie Prezentownik to: <strong>{resetCode}</strong></p>",
            $"Twój kod do zresetowania hasła w serwisie Prezentownik to: {resetCode}");
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

    private async Task SendAsync(string email, string subject, string htmlBody, string plainTextBody)
    {
        var message = new EmailMessage(
            senderAddress: _senderAddress,
            recipients: new EmailRecipients([new EmailAddress(email)]),
            content: new EmailContent(subject)
            {
                Html = htmlBody,
                PlainText = plainTextBody
            });

        Log.Information("Sending email from {SenderEmail} to {RecipientEmail}, subject: {Subject}, plain-text body: {PlainTextBody}",
            _senderAddress,
            email,
            subject,
            plainTextBody);

        await _emailClient.SendAsync(WaitUntil.Started, message);
    }
}
