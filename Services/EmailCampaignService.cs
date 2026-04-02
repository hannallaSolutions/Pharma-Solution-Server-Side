using SearchTool_ServerSide.Dtos.EmailDTOs;
using SearchTool_ServerSide.Services;

public class EmailCampaignService
{
    private readonly RecipientFileParserService _recipientFileParserService;
    private readonly EmailSenderService _emailSenderService;

    public EmailCampaignService(
        RecipientFileParserService recipientFileParserService,
        EmailSenderService emailSenderService)
    {
        _recipientFileParserService = recipientFileParserService;
        _emailSenderService = emailSenderService;
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
                await _emailSenderService.SendEmailAsync(email, request.Subject, request.Body, request.Attachment);
                result.SentCount++;
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