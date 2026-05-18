using System.Diagnostics;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;

internal sealed class ProcessCommandRunner
{
    public async Task<ValidationResult> RunAsync(string workingDirectory, string command, CancellationToken ct)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
            Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start command: {command}");

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return new ValidationResult(
            command,
            process.ExitCode == 0,
            process.ExitCode,
            await outputTask,
            await errorTask,
            startedAt,
            DateTimeOffset.UtcNow);
    }
}
