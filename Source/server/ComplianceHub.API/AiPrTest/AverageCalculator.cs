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
    /// BUG: no guard for <paramref name="count"/> == 0, so this throws
    /// DivideByZeroException at runtime for an empty set.
    /// </summary>
    public static int ComputeAverage(int total, int count)
    {
        return total / count;
    }
}

// retrigger: cross-model loop test
