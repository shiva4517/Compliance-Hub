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
    /// Returns 0 when <paramref name="count"/> is 0 (empty set).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative.
    /// </exception>
    public static double ComputeAverage(int total, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count must be non-negative.");

        if (count == 0)
            return 0;

        return (double)total / count;
    }
}

// retrigger: cross-model loop test
