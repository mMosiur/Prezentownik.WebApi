using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.Auth.Email;
using Prezentownik.WebApi.Services.Email;
using Xunit;

namespace Prezentownik.WebApi.Tests;

public class IdentityEmailSenderTests
{
    private sealed class FakeEmailService : IEmailService
    {
        public string? LastRecipient { get; private set; }
        public string? LastSubject { get; private set; }
        public string? LastHtmlBody { get; private set; }
        public string? LastPlainTextBody { get; private set; }

        public Task SendAsync(string recipient, string subject, string htmlBody, string? plainTextBody = null, CancellationToken cancellationToken = default)
        {
            LastRecipient = recipient;
            LastSubject = subject;
            LastHtmlBody = htmlBody;
            LastPlainTextBody = plainTextBody;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTemplateService : IIdentityEmailTemplateService
    {
        public string? RenderedConfirmationLink { get; private set; }
        public string? RenderedPasswordResetLink { get; private set; }

        public EmailBody RenderEmailConfirmation(string confirmationLink)
        {
            RenderedConfirmationLink = confirmationLink;
            return new($"<html>{confirmationLink}</html>", confirmationLink);
        }

        public EmailBody RenderEmailPasswordResetLink(string resetLink)
        {
            RenderedPasswordResetLink = resetLink;
            return new($"<html>{resetLink}</html>", resetLink);
        }
    }

    private static IConfiguration CreateConfiguration(string frontendBaseUrl)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = frontendBaseUrl
            })
            .Build();
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_BuildsFrontendUrl_AndSendsEmail()
    {
        var config = CreateConfiguration("https://prezentownik.info.pl");
        var fakeTemplateService = new FakeTemplateService();
        var fakeEmailService = new FakeEmailService();
        var sender = new IdentityEmailSender(config, fakeTemplateService, fakeEmailService);

        var user = new AppUser { Email = "user@example.com" };
        var apiConfirmationLink = "http://api.local/auth/confirmEmail?userId=user123&code=secretCode%3D";

        await sender.SendConfirmationLinkAsync(user, "user@example.com", apiConfirmationLink);

        Assert.Equal("user@example.com", fakeEmailService.LastRecipient);
        Assert.Equal("Potwierdź swój adres e-mail", fakeEmailService.LastSubject);
        Assert.NotNull(fakeEmailService.LastHtmlBody);
        Assert.NotNull(fakeEmailService.LastPlainTextBody);
        Assert.NotNull(fakeTemplateService.RenderedConfirmationLink);
        Assert.StartsWith("https://prezentownik.info.pl/confirm-email", fakeTemplateService.RenderedConfirmationLink);
        Assert.Contains("userId=user123", fakeTemplateService.RenderedConfirmationLink);
        Assert.Contains("code=secretCode%3D", fakeTemplateService.RenderedConfirmationLink);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_BuildsFrontendResetPasswordUrl_AndSendsLinkEmail()
    {
        var config = CreateConfiguration("https://prezentownik.info.pl");
        var fakeTemplateService = new FakeTemplateService();
        var fakeEmailService = new FakeEmailService();
        var sender = new IdentityEmailSender(config, fakeTemplateService, fakeEmailService);

        var email = "user+tag@example.com";
        var resetCode = "encoded+&Reset=Code";

        var user = new AppUser { Email = email };
        await sender.SendPasswordResetCodeAsync(user, email, resetCode);

        Assert.Equal(email, fakeEmailService.LastRecipient);
        Assert.Equal("Zresetuj swoje hasło", fakeEmailService.LastSubject);
        Assert.NotNull(fakeEmailService.LastHtmlBody);
        Assert.NotNull(fakeEmailService.LastPlainTextBody);
        Assert.NotNull(fakeTemplateService.RenderedPasswordResetLink);
        var query = QueryHelpers.ParseQuery(new Uri(fakeTemplateService.RenderedPasswordResetLink!).Query);
        Assert.Equal(email, query["email"].ToString());
        Assert.Equal(resetCode, query["code"].ToString());
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_BuildsFrontendResetPasswordUrl_AndSendsLinkEmail()
    {
        var config = CreateConfiguration("https://prezentownik.info.pl");
        var fakeTemplateService = new FakeTemplateService();
        var fakeEmailService = new FakeEmailService();
        var sender = new IdentityEmailSender(config, fakeTemplateService, fakeEmailService);

        var email = "user+tag@example.com";
        var user = new AppUser { Email = email };
        var apiResetLink = "http://api.local/auth/resetPassword?userId=user123&email=old%40example.com&code=abc%2B123%3D%26";

        await sender.SendPasswordResetLinkAsync(user, email, apiResetLink);

        Assert.Equal(email, fakeEmailService.LastRecipient);
        Assert.Equal("Zresetuj swoje hasło", fakeEmailService.LastSubject);
        Assert.NotNull(fakeEmailService.LastHtmlBody);
        Assert.NotNull(fakeEmailService.LastPlainTextBody);
        Assert.NotNull(fakeTemplateService.RenderedPasswordResetLink);
        Assert.StartsWith("https://prezentownik.info.pl/reset-password", fakeTemplateService.RenderedPasswordResetLink);
        var query = QueryHelpers.ParseQuery(new Uri(fakeTemplateService.RenderedPasswordResetLink!).Query);
        Assert.Equal("user123", query["userId"].ToString());
        Assert.Equal(email, query["email"].ToString());
        Assert.Equal("abc+123=&", query["code"].ToString());
    }
}
