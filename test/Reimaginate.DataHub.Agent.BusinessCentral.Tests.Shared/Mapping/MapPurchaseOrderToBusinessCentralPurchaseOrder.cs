using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrder;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrder;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapPurchaseOrderToBusinessCentralPurchaseOrder :
    ITypeMapper<DataHubPurchaseOrder, BusinessCentralPurchaseOrder>,
    IDataHubTypeMapper<DataHubPurchaseOrder, BusinessCentralPurchaseOrder>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DataHubPurchaseOrder.Supplier)];

    public Task<BusinessCentralPurchaseOrder> MapAsync(
        DataHubPurchaseOrder from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Supplier is null)
        {
            throw new InvalidOperationException(
                "A Data Hub purchase order must reference a supplier before it can be synced.");
        }

        var vendorId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSupplier>(
            from.Supplier,
            typeof(BusinessCentralVendor).Name,
            cache);
        if (!vendorId.HasValue)
        {
            throw new InvalidOperationException(
                $"The supplier referenced by purchase order '{from.id}' has no Business Central vendor id.");
        }

        return Task.FromResult(new BusinessCentralPurchaseOrder
        {
            DataHubCorrelationId = BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubPurchaseOrder), from.id),
            OrderDate = from.OrderDate,
            PostingDate = from.PostingDate,
            VendorId = vendorId,
            RequestedReceiptDate = from.RequestedReceiptDate
        });
    }
}
