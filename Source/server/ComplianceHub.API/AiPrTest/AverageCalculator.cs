namespace ComplianceHub.API.AiPrTest;

/// <summary>
/// TEMPORARY test helper used to verify the AI PR automation loop.
/// Contains an intentional, obvious defect for the reviewer agent to catch.
/// Delete this file once the workflow has been validated.
/// </summary>
public static class AverageCalculator
{
    /// <summary>
    /// Computes the average of a total over a count of items.
    /// </summary>
    /// <param name="total">The sum of the items.</param>
    /// <param name="count">The number of items. Must be non-negative and non-zero.</param>
    /// <returns>The average as a double.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="count"/> is zero.</exception>
    public static double ComputeAverage(long total, long count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        }

        if (count == 0)
        {
            throw new ArgumentException("Count cannot be zero, as an average for zero items is undefined.", nameof(count));
        }

        return (double)total / count;
    }
}