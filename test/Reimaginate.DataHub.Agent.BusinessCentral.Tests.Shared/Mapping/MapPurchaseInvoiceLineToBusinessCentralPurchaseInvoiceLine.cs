using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapPurchaseInvoiceLineToBusinessCentralPurchaseInvoiceLine :
    ITypeMapper<DataHubPurchaseInvoiceLine, BusinessCentralPurchaseInvoiceLine>,
    IDataHubTypeMapper<DataHubPurchaseInvoiceLine, BusinessCentralPurchaseInvoiceLine>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubPurchaseInvoiceLine.PurchaseInvoice),
        nameof(DataHubPurchaseInvoiceLine.Product)
    ];

    public Task<BusinessCentralPurchaseInvoiceLine> MapAsync(
        DataHubPurchaseInvoiceLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.PurchaseInvoice is null || from.Product is null)
        {
            throw new InvalidOperationException(
                "A Data Hub purchase invoice line must reference both a purchase invoice and a product before it can be synced.");
        }

        var documentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubPurchaseInvoice>(
            from.PurchaseInvoice,
            typeof(BusinessCentralPurchaseInvoice).Name,
            cache);
        var itemId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubProduct>(
            from.Product,
            typeof(BusinessCentralItem).Name,
            cache);
        if (!documentId.HasValue || !itemId.HasValue)
        {
            throw new InvalidOperationException(
                $"Purchase invoice line '{from.id}' could not resolve its Business Central invoice and item references.");
        }

        var businessCentralLine = new BusinessCentralPurchaseInvoiceLine
        {
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitCost = from.UnitCost,
            ExpectedReceiptDate = from.ExpectedReceiptDate
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
