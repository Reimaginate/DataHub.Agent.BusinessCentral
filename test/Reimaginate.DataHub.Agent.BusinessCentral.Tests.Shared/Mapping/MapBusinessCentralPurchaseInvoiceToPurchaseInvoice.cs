using Reimaginate.Mapper;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseInvoiceToPurchaseInvoice :
    ITypeMapper<BusinessCentralPurchaseInvoice, DataHubPurchaseInvoice>
{
    public Task<DataHubPurchaseInvoice> MapAsync(
        BusinessCentralPurchaseInvoice from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubPurchaseInvoice
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            InvoiceNumber = from.Number,
            VendorInvoiceNumber = from.VendorInvoiceNumber,
            PostingDate = from.PostingDate,
            InvoiceDate = from.InvoiceDate,
            DueDate = from.DueDate,
            Supplier = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSupplier, BusinessCentralVendor>(from.VendorId),
            Status = from.Status,
            PricesIncludeTax = from.PricesIncludeTax,
            DiscountAmount = from.DiscountAmount,
            DiscountAppliedBeforeTax = from.DiscountAppliedBeforeTax,
            TotalAmountExcludingTax = from.TotalAmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            TotalAmountIncludingTax = from.TotalAmountIncludingTax
        });
    }
}
