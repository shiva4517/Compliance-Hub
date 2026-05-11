using ComplianceHub.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Functions.Functions;

public class RegulationSyncTimerFunction(
    RegulationSyncService syncService,
    ILogger<RegulationSyncTimerFunction> logger)
{
    [Function("RegulationSyncTimer")]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer,
        FunctionContext context)
    {
        logger.LogInformation("RegulationSyncTimer triggered at {Time}", DateTime.UtcNow);
        await syncService.RunAsync(CancellationToken.None);
        logger.LogInformation("RegulationSyncTimer completed at {Time}", DateTime.UtcNow);
    }
}
