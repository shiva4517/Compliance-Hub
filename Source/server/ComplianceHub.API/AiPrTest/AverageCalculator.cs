namespace ComplianceHub.API.AiPrTest;

/// <summary>
/// TEMPORARY test helper used to verify the AI PR automation loop.
/// Contains an intentional, obvious defect for the reviewer agent to catch.
/// Delete this file once the workflow has been validated.
/// </summary>
public static class AverageCalculator
{
    /// <summary>
    /// Returns the average of a total over a count of items.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="count"/> is zero.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is 0.</exception>
    public static int ComputeAverage(int total, int count)
    {
        if (count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be zero when computing an average.");
        }
        return total / count;
    }
}