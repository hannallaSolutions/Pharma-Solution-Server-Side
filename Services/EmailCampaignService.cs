using SearchTool_ServerSide.Dtos.EmailDTOs;
using SearchTool_ServerSide.Services;

public class EmailCampaignService
{
    private readonly RecipientFileParserService _recipientFileParserService;
    private readonly EmailSenderService _emailSenderService;

private readonly LocalEmailValidationService _localValidation;

public EmailCampaignService(
    RecipientFileParserService recipientFileParserService,
    EmailSenderService emailSenderService,
    LocalEmailValidationService localValidation)
{
    _recipientFileParserService = recipientFileParserService;
    _emailSenderService = emailSenderService;
    _localValidation = localValidation;
}

    public async Task<BulkEmailResultDto> SendBulkEmailAsync(BulkEmailRequestDto request)
    {
        var emails = await _recipientFileParserService.ExtractEmailsAsync(request.RecipientsFile);

        var result = new BulkEmailResultDto();
        var distinctEmails = emails.Distinct().ToList();

      foreach (var email in distinctEmails)
{
    try
    {
        // 1) Format
        if (!_localValidation.IsValidFormat(email))
        {
            result.InvalidEmails.Add(email);
            result.FailedCount++;
            continue;
        }

        // 2) Domain check
        if (!await _localValidation.DomainExistsAsync(email))
        {
            result.InvalidEmails.Add(email);
            result.FailedCount++;
            continue;
        }

        // 3) Disposable
        if (_localValidation.IsDisposable(email))
        {
            result.InvalidEmails.Add(email);
            result.FailedCount++;
            continue;
        }

        // ✅ لو عدى كل ده → ابعت
        await _emailSenderService.SendEmailAsync(
            email,
            request.Subject,
            request.Body,
            request.Attachment);

        result.SentCount++;
        result.ValidEmailsCount++;
    }
    catch (Exception ex)
    {
        result.FailedCount++;
        result.Failures.Add(new EmailSendFailureDto
        {
            Email = email,
            Reason = ex.Message
        });
    }
}
        result.TotalRows = emails.Count;
        result.ValidEmailsCount = distinctEmails.Count;

        return result;
    }
}