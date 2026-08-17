using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public static class BusinessCentralSalesInvoiceCleanupPolicy
{
    public static bool IsSafeEmptyPostedTestArtifact(
        BusinessCentralSalesInvoice invoice,
        IReadOnlyCollection<BusinessCentralSalesInvoiceLine> lines)
    {
        if (!Guid.TryParse(invoice.Id, out var invoiceId) ||
            !string.Equals(invoice.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
            invoice.ExternalDocumentNumber?.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase) != true ||
            invoice.RemainingAmount != 0m ||
            invoice.TotalAmountExcludingTax != 0m ||
            invoice.TotalTaxAmount != 0m ||
            invoice.TotalAmountIncludingTax != 0m)
        {
            return false;
        }

        return lines.All(line =>
            line.DocumentId == invoiceId &&
            line.Quantity == 0m &&
            line.UnitPrice == 0m &&
            line.AmountExcludingTax == 0m &&
            line.TotalTaxAmount == 0m &&
            line.AmountIncludingTax == 0m);
    }
}
