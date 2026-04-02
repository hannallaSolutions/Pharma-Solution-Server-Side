public class BulkEmailResultDto
{
    public int TotalRows { get; set; }
    public int ValidEmailsCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    public List<string> InvalidEmails { get; set; } = new();
    public List<EmailSendFailureDto> Failures { get; set; } = new();
}

public class EmailSendFailureDto
{
    public string Email { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}