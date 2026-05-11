using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace ComplianceHub.Functions.Telemetry;

public class AppInsightsTelemetry(TelemetryClient client)
{
    public void TrackEvent(string name, IDictionary<string, string>? properties = null)
        => client.TrackEvent(name, properties);

    public void TrackException(Exception ex, IDictionary<string, string>? properties = null)
        => client.TrackException(ex, properties);

    public void TrackDependency(string type, string target, string name, bool success, TimeSpan duration)
    {
        var dep = new DependencyTelemetry
        {
            Type = type,
            Target = target,
            Name = name,
            Success = success,
            Duration = duration
        };
        client.TrackDependency(dep);
    }

    public void Flush() => client.Flush();
}
