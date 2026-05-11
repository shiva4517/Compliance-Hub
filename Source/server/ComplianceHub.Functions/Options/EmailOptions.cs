namespace ComplianceHub.Functions.Options;

public class EmailOptions
{
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Compliance Hub";
}
