using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;
using DataHubSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSalesInvoiceLineToBusinessCentralSalesInvoiceLine :
    ITypeMapper<DataHubSalesInvoiceLine, BusinessCentralSalesInvoiceLine>,
    IDataHubTypeMapper<DataHubSalesInvoiceLine, BusinessCentralSalesInvoiceLine>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubSalesInvoiceLine.SalesInvoice),
        nameof(DataHubSalesInvoiceLine.Product)
    ];

    public Task<BusinessCentralSalesInvoiceLine> MapAsync(
        DataHubSalesInvoiceLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.SalesInvoice is null || from.Product is null)
        {
            throw new InvalidOperationException(
                "A Data Hub sales invoice line must reference both a sales invoice and a product before it can be synced.");
        }

        var documentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSalesInvoice>(
            from.SalesInvoice,
            typeof(BusinessCentralSalesInvoice).Name,
            cache);
        var itemId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubProduct>(
            from.Product,
            typeof(BusinessCentralItem).Name,
            cache);
        if (!documentId.HasValue || !itemId.HasValue)
        {
            throw new InvalidOperationException(
                $"Sales invoice line '{from.id}' could not resolve its Business Central invoice and item references.");
        }

        var businessCentralLine = new BusinessCentralSalesInvoiceLine
        {
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice
        };

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
