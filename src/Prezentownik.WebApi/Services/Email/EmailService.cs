using Azure;
using Azure.Communication.Email;

namespace Prezentownik.WebApi.Services.Email;

public sealed partial class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _senderAddress;
    private readonly EmailClient _emailClient;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _senderAddress = configuration["AzureCommunicationServices:SenderAddress"] ?? "donotreply@dummy.com";
        var connectionString = configuration["AzureCommunicationServices:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "endpoint=https://dummy.communication.azure.com/;accesskey=dummy";
        }

        _emailClient = new(connectionString);
    }

    public async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        string plainTextBody,
        CancellationToken cancellationToken)
    {
        var message = new EmailMessage(
            senderAddress: _senderAddress,
            recipientAddress: recipient,
            content: new EmailContent(subject)
            {
                Html = htmlBody,
                PlainText = plainTextBody
            });

        LogSendingEmail(_senderAddress, recipient, subject, plainTextBody);

        await _emailClient.SendAsync(WaitUntil.Started, message, cancellationToken);
    }

    [LoggerMessage(LogLevel.Information,
        "Sending email from {SenderEmail} to {RecipientEmail}, subject: {Subject}, plain-text body: {PlainTextBody}")]
    partial void LogSendingEmail(
        string senderEmail,
        string recipientEmail,
        string subject,
        string plainTextBody);
}
