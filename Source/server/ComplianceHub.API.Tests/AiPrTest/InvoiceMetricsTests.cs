using ComplianceHub.API.AiPrTest;
using Xunit;

namespace ComplianceHub.API.Tests.AiPrTest;

public class InvoiceMetricsTests
{
    [Fact]
    public void AverageInvoiceAmount_ReturnsCorrectAverage()
    {
        var result = InvoiceMetrics.AverageInvoiceAmount(300m, 3);
        Assert.Equal(100m, result);
    }

    [Fact]
    public void AverageInvoiceAmount_ZeroInvoices_ReturnsZero()
    {
        var result = InvoiceMetrics.AverageInvoiceAmount(500m, 0);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void AverageInvoiceAmount_SingleInvoice_ReturnsTotalAmount()
    {
        var result = InvoiceMetrics.AverageInvoiceAmount(250m, 1);
        Assert.Equal(250m, result);
    }
}
