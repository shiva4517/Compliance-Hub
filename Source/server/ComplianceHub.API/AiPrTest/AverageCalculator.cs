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
    /// <returns>The average as a double, or 0 if count is zero.</returns>
    public static double ComputeAverage(int total, int count)
    {
        if (count == 0)
        {
            return 0.0;
        }
        return (double)total / count;
    }
}