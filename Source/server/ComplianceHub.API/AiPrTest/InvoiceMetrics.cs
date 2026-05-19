namespace ComplianceHub.API.AiPrTest;

/// <summary>
/// Computes simple metrics over a set of invoices.
/// </summary>
public static class InvoiceMetrics
{
    /// <summary>
    /// Returns the average invoice amount for a billing period.
    /// </summary>
    /// <param name="totalAmount">Sum of all invoice amounts in the period.</param>
    /// <param name="invoiceCount">Number of invoices in the period.</param>
    /// <returns>The average amount per invoice.</returns>
    public static decimal AverageInvoiceAmount(decimal totalAmount, int invoiceCount)
    {
        return totalAmount / invoiceCount;
    }
}
