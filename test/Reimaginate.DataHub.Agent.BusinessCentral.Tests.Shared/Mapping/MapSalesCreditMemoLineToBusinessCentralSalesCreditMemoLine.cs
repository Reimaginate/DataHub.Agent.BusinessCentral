using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemo;
using DataHubSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemoLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSalesCreditMemoLineToBusinessCentralSalesCreditMemoLine :
    ITypeMapper<DataHubSalesCreditMemoLine, BusinessCentralSalesCreditMemoLine>,
    IDataHubTypeMapper<DataHubSalesCreditMemoLine, BusinessCentralSalesCreditMemoLine>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubSalesCreditMemoLine.SalesCreditMemo),
        nameof(DataHubSalesCreditMemoLine.Product)
    ];

    public Task<BusinessCentralSalesCreditMemoLine> MapAsync(
        DataHubSalesCreditMemoLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.SalesCreditMemo is null || from.Product is null)
        {
            throw new InvalidOperationException(
                "A Data Hub sales credit memo line must reference both a sales credit memo and a product before it can be synced.");
        }

        var documentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSalesCreditMemo>(
            from.SalesCreditMemo,
            typeof(BusinessCentralSalesCreditMemo).Name,
            cache);
        var itemId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubProduct>(
            from.Product,
            typeof(BusinessCentralItem).Name,
            cache);
        if (!documentId.HasValue || !itemId.HasValue)
        {
            throw new InvalidOperationException(
                $"Sales credit memo line '{from.id}' could not resolve its Business Central credit memo and item references.");
        }

        var result = new BusinessCentralSalesCreditMemoLine
        {
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice,
            ShipmentDate = from.ShipmentDate
        };

        if (from.DiscountPercent.HasValue || !from.ManualDiscountAmount.HasValue)
        {
            result.DiscountPercent = from.DiscountPercent ?? 0m;
        }
        else
        {
            result.DiscountAmount = from.ManualDiscountAmount;
        }

        return Task.FromResult(result);
    }
}
