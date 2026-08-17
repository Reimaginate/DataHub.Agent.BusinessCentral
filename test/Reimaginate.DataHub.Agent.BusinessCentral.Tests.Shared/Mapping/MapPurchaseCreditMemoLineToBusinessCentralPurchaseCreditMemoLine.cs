using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemoLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemo;
using DataHubPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemoLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapPurchaseCreditMemoLineToBusinessCentralPurchaseCreditMemoLine :
    ITypeMapper<DataHubPurchaseCreditMemoLine, BusinessCentralPurchaseCreditMemoLine>,
    IDataHubTypeMapper<DataHubPurchaseCreditMemoLine, BusinessCentralPurchaseCreditMemoLine>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubPurchaseCreditMemoLine.PurchaseCreditMemo),
        nameof(DataHubPurchaseCreditMemoLine.Product)
    ];

    public Task<BusinessCentralPurchaseCreditMemoLine> MapAsync(
        DataHubPurchaseCreditMemoLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.PurchaseCreditMemo is null || from.Product is null)
        {
            throw new InvalidOperationException(
                "A Data Hub purchase credit memo line must reference both a purchase credit memo and a product before it can be synced.");
        }

        var documentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubPurchaseCreditMemo>(
            from.PurchaseCreditMemo,
            typeof(BusinessCentralPurchaseCreditMemo).Name,
            cache);
        var itemId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubProduct>(
            from.Product,
            typeof(BusinessCentralItem).Name,
            cache);
        if (!documentId.HasValue || !itemId.HasValue)
        {
            throw new InvalidOperationException(
                $"Purchase credit memo line '{from.id}' could not resolve its Business Central memo and item references.");
        }

        var result = new BusinessCentralPurchaseCreditMemoLine
        {
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Quantity = from.Quantity,
            UnitCost = from.UnitCost
        };

        if (from.DiscountPercent.HasValue || !from.DiscountAmount.HasValue)
        {
            result.DiscountPercent = from.DiscountPercent ?? 0m;
        }
        else
        {
            result.DiscountAmount = from.DiscountAmount;
        }

        return Task.FromResult(result);
    }
}
