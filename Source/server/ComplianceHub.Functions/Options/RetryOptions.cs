namespace ComplianceHub.Functions.Options;

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelaySeconds { get; set; } = 2;
}
