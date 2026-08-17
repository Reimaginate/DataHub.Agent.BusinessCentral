using Reimaginate.Mapper;
using BusinessCentralPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrder;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrder;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseOrderToPurchaseOrder :
    ITypeMapper<BusinessCentralPurchaseOrder, DataHubPurchaseOrder>
{
    public Task<DataHubPurchaseOrder> MapAsync(
        BusinessCentralPurchaseOrder from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubPurchaseOrder
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            OrderNumber = from.Number,
            OrderDate = from.OrderDate,
            PostingDate = from.PostingDate,
            Supplier = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSupplier, BusinessCentralVendor>(from.VendorId),
            RequestedReceiptDate = from.RequestedReceiptDate,
            DiscountAmount = from.DiscountAmount,
            PricesIncludeTax = from.PricesIncludeTax,
            Status = from.Status,
            FullyReceived = from.FullyReceived,
            TotalAmountExcludingTax = from.TotalAmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            TotalAmountIncludingTax = from.TotalAmountIncludingTax
        });
    }
}
