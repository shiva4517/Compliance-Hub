using ComplianceHub.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Functions.Functions;

public class OutboxProcessorTimerFunction(
    OutboxProcessorService processorService,
    ILogger<OutboxProcessorTimerFunction> logger)
{
    [Function("OutboxProcessor")]
    public async Task Run(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        FunctionContext context)
    {
        logger.LogInformation("OutboxProcessor triggered at {Time}", DateTime.UtcNow);
        await processorService.ProcessAsync(CancellationToken.None);
        logger.LogInformation("OutboxProcessor completed at {Time}", DateTime.UtcNow);
    }
}
