using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrder;
using BusinessCentralSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrderLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrder;
using DataHubSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSalesOrderLineToBusinessCentralSalesOrderLine :
    ITypeMapper<DataHubSalesOrderLine, BusinessCentralSalesOrderLine>,
    IDataHubTypeMapper<DataHubSalesOrderLine, BusinessCentralSalesOrderLine>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubSalesOrderLine.SalesOrder),
        nameof(DataHubSalesOrderLine.Product)
    ];

    public Task<BusinessCentralSalesOrderLine> MapAsync(
        DataHubSalesOrderLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.SalesOrder is null || from.Product is null)
        {
            throw new InvalidOperationException(
                "A Data Hub sales order line must reference both a sales order and a product before it can be synced.");
        }

        var documentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSalesOrder>(
            from.SalesOrder,
            typeof(BusinessCentralSalesOrder).Name,
            cache);
        var itemId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubProduct>(
            from.Product,
            typeof(BusinessCentralItem).Name,
            cache);

        if (!documentId.HasValue || !itemId.HasValue)
        {
            throw new InvalidOperationException(
                $"Sales order line '{from.id}' could not resolve its Business Central order and item references.");
        }

        var businessCentralLine = new BusinessCentralSalesOrderLine
        {
            DataHubCorrelationId = BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrderLine), from.id),
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice
        };

        // Business Central calculates the other discount representation whenever one is
        // supplied. Mapping both fields makes that calculated value repeatedly differ from
        // a null Data Hub value. Prefer percentage when present (including an explicit zero),
        // otherwise use amount; clearing both representations means zero percent.
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
