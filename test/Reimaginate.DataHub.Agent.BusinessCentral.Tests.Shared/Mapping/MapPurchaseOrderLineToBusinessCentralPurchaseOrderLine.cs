using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrder;
using BusinessCentralPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrderLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrder;
using DataHubPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapPurchaseOrderLineToBusinessCentralPurchaseOrderLine :
    ITypeMapper<DataHubPurchaseOrderLine, BusinessCentralPurchaseOrderLine>,
    IDataHubTypeMapper<DataHubPurchaseOrderLine, BusinessCentralPurchaseOrderLine>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubPurchaseOrderLine.PurchaseOrder),
        nameof(DataHubPurchaseOrderLine.Product)
    ];

    public Task<BusinessCentralPurchaseOrderLine> MapAsync(
        DataHubPurchaseOrderLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.PurchaseOrder is null || from.Product is null)
        {
            throw new InvalidOperationException(
                "A Data Hub purchase order line must reference both a purchase order and a product before it can be synced.");
        }

        var documentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubPurchaseOrder>(
            from.PurchaseOrder,
            typeof(BusinessCentralPurchaseOrder).Name,
            cache);
        var itemId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubProduct>(
            from.Product,
            typeof(BusinessCentralItem).Name,
            cache);

        if (!documentId.HasValue || !itemId.HasValue)
        {
            throw new InvalidOperationException(
                $"Purchase order line '{from.id}' could not resolve its Business Central order and item references.");
        }

        var businessCentralLine = new BusinessCentralPurchaseOrderLine
        {
            DataHubCorrelationId = BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubPurchaseOrderLine), from.id),
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            DirectUnitCost = from.DirectUnitCost
        };

        // Business Central recalculates an item line's expected receipt date when its undefined
        // date is supplied. Treat a missing Data Hub value as unspecified so the calculated date
        // is preserved and subsequent syncs do not continually reapply an impossible clear.
        if (!string.IsNullOrWhiteSpace(from.ExpectedReceiptDate))
        {
            businessCentralLine.ExpectedReceiptDate = from.ExpectedReceiptDate;
        }

        if (from.DiscountPercent.HasValue || !from.DiscountAmount.HasValue)
        {
            businessCentralLine.DiscountPercent = from.DiscountPercent ?? 0m;
        }
        else
        {
            businessCentralLine.DiscountAmount = from.DiscountAmount;
        }

        return Task.FromResult(businessCentralLine);
    }
}
