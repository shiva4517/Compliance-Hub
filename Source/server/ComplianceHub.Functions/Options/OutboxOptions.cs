namespace ComplianceHub.Functions.Options;

public class OutboxOptions
{
    public int PollingIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 10;
    public int StaleProcessingThresholdMinutes { get; set; } = 5;
}
