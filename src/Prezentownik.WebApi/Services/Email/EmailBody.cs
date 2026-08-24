namespace Prezentownik.WebApi.Services.Email;

public record struct EmailBody(
    string Html,
    string PlainText);
