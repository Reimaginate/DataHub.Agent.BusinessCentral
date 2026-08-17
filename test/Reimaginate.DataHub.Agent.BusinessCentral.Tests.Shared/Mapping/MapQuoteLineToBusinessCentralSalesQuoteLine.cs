using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuote;
using BusinessCentralSalesQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuoteLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Quote;
using DataHubQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.QuoteLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapQuoteLineToBusinessCentralSalesQuoteLine :
    ITypeMapper<DataHubQuoteLine, BusinessCentralSalesQuoteLine>,
    IDataHubTypeMapper<DataHubQuoteLine, BusinessCentralSalesQuoteLine>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubQuoteLine.Quote),
        nameof(DataHubQuoteLine.Product)
    ];

    public Task<BusinessCentralSalesQuoteLine> MapAsync(
        DataHubQuoteLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Quote is null || from.Product is null)
        {
            throw new InvalidOperationException(
                "A Data Hub quote line must reference both a quote and a product before it can be synced.");
        }

        var documentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubQuote>(
            from.Quote,
            typeof(BusinessCentralSalesQuote).Name,
            cache);
        var itemId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubProduct>(
            from.Product,
            typeof(BusinessCentralItem).Name,
            cache);
        if (!documentId.HasValue || !itemId.HasValue)
        {
            throw new InvalidOperationException(
                $"Quote line '{from.id}' could not resolve its Business Central quote and item references.");
        }

        var businessCentralLine = new BusinessCentralSalesQuoteLine
        {
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitPrice = from.PricePerUnit
        };

        if (from.DiscountPercent.HasValue || !from.ManualDiscountAmount.HasValue)
        {
            businessCentralLine.DiscountPercent = from.DiscountPercent ?? 0m;
        }
        else
        {
            businessCentralLine.DiscountAmount = from.ManualDiscountAmount;
        }

        return Task.FromResult(businessCentralLine);
    }
}
