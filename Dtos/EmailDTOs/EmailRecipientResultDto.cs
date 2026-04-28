public class EmailRecipientResultDto
{
    public string Email { get; set; } = string.Empty;
    public EmailDeliveryStatus Status { get; set; }
    public string? Reason { get; set; }
    public bool ExternallyVerified { get; set; }
}