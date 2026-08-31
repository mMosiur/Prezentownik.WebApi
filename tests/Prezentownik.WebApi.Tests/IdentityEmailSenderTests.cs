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

        public Task<EmailBody> RenderEmailConfirmationAsync(string confirmationLink)
        {
            RenderedConfirmationLink = confirmationLink;
            return Task.FromResult(new EmailBody($"<html>{confirmationLink}</html>", confirmationLink));
        }

        public Task<EmailBody> RenderEmailPasswordResetLinkAsync(string resetLink)
        {
            RenderedPasswordResetLink = resetLink;
            return Task.FromResult(new EmailBody($"<html>{resetLink}</html>", resetLink));
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

        var user = new AppUser { Email = "user@example.com" };
        var resetCode = "encodedResetCode123";

        await sender.SendPasswordResetCodeAsync(user, "user@example.com", resetCode);

        Assert.Equal("user@example.com", fakeEmailService.LastRecipient);
        Assert.Equal("Zresetuj swoje hasło", fakeEmailService.LastSubject);
        Assert.NotNull(fakeEmailService.LastHtmlBody);
        Assert.NotNull(fakeEmailService.LastPlainTextBody);
        Assert.NotNull(fakeTemplateService.RenderedPasswordResetLink);
        Assert.Equal("https://prezentownik.info.pl/reset-password?email=user@example.com&code=encodedResetCode123", fakeTemplateService.RenderedPasswordResetLink);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_BuildsFrontendResetPasswordUrl_AndSendsLinkEmail()
    {
        var config = CreateConfiguration("https://prezentownik.info.pl");
        var fakeTemplateService = new FakeTemplateService();
        var fakeEmailService = new FakeEmailService();
        var sender = new IdentityEmailSender(config, fakeTemplateService, fakeEmailService);

        var user = new AppUser { Email = "user@example.com" };
        var apiResetLink = "http://api.local/auth/resetPassword?email=user@example.com&code=abc123Code";

        await sender.SendPasswordResetLinkAsync(user, "user@example.com", apiResetLink);

        Assert.Equal("user@example.com", fakeEmailService.LastRecipient);
        Assert.Equal("Zresetuj swoje hasło", fakeEmailService.LastSubject);
        Assert.NotNull(fakeEmailService.LastHtmlBody);
        Assert.NotNull(fakeEmailService.LastPlainTextBody);
        Assert.NotNull(fakeTemplateService.RenderedPasswordResetLink);
        Assert.StartsWith("https://prezentownik.info.pl/reset-password", fakeTemplateService.RenderedPasswordResetLink);
        Assert.Contains("email=user@example.com", fakeTemplateService.RenderedPasswordResetLink);
        Assert.Contains("code=abc123Code", fakeTemplateService.RenderedPasswordResetLink);
    }
}
