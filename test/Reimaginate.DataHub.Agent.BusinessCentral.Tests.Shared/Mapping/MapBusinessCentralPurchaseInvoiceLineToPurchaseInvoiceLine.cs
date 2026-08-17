using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseInvoiceLineToPurchaseInvoiceLine :
    ITypeMapper<BusinessCentralPurchaseInvoiceLine, DataHubPurchaseInvoiceLine>
{
    public Task<DataHubPurchaseInvoiceLine> MapAsync(
        BusinessCentralPurchaseInvoiceLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubPurchaseInvoiceLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            PurchaseInvoice = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>(from.DocumentId),
            Product = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubProduct, BusinessCentralItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitCost = from.UnitCost,
            DiscountAmount = from.DiscountAmount,
            DiscountPercent = from.DiscountPercent,
            DiscountAppliedBeforeTax = from.DiscountAppliedBeforeTax,
            AmountExcludingTax = from.AmountExcludingTax,
            TaxPercent = from.TaxPercent,
            TotalTaxAmount = from.TotalTaxAmount,
            AmountIncludingTax = from.AmountIncludingTax,
            ExpectedReceiptDate = from.ExpectedReceiptDate
        });
    }
}
