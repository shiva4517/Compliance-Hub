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
    /// Handles cases where <paramref name="count"/> is zero by returning 0 to prevent
    /// a DivideByZeroException.
    /// </summary>
    public static int ComputeAverage(int total, int count)
    {
        if (count == 0)
        {
            return 0;
        }
        return total / count;
    }
}