namespace Prezentownik.WebApi.Services.Email;

public sealed partial class LoggingOnlyEmailService : IEmailService
{
    private readonly ILogger<LoggingOnlyEmailService> _logger;
    private readonly string? _senderAddress;

    public LoggingOnlyEmailService(ILogger<LoggingOnlyEmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _senderAddress = configuration["AzureCommunicationServices:SenderAddress"];
    }

    public Task SendAsync(string recipient, string subject, string htmlBody, string plainTextBody, CancellationToken cancellationToken)
    {
        LogSendingEmail(_senderAddress ?? "<null>", recipient, subject, plainTextBody);
        var tempFileName = Path.GetTempFileName() + ".html";
        File.WriteAllText(tempFileName, htmlBody);
        LogHtmlFileLocation(tempFileName);
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Information,
        "Sending email from {SenderEmail} to {RecipientEmail}, subject: {Subject}, plain-text body: {PlainTextBody}")]
    partial void LogSendingEmail(
        string senderEmail,
        string recipientEmail,
        string subject,
        string plainTextBody);

    [LoggerMessage(LogLevel.Information,
        "Rendered HTML file location: {HtmlFileLocation}")]
    partial void LogHtmlFileLocation(
        string htmlFileLocation);
}
