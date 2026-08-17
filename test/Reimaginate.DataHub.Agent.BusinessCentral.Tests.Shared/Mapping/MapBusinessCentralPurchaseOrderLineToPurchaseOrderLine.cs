using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrder;
using BusinessCentralPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrderLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrder;
using DataHubPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseOrderLineToPurchaseOrderLine :
    ITypeMapper<BusinessCentralPurchaseOrderLine, DataHubPurchaseOrderLine>
{
    public Task<DataHubPurchaseOrderLine> MapAsync(
        BusinessCentralPurchaseOrderLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubPurchaseOrderLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            PurchaseOrder = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubPurchaseOrder, BusinessCentralPurchaseOrder>(from.DocumentId),
            Product = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubProduct, BusinessCentralItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            DirectUnitCost = from.DirectUnitCost,
            DiscountAmount = from.DiscountAmount,
            DiscountPercent = from.DiscountPercent,
            AmountExcludingTax = from.AmountExcludingTax,
            TaxPercent = from.TaxPercent,
            TotalTaxAmount = from.TotalTaxAmount,
            AmountIncludingTax = from.AmountIncludingTax,
            ExpectedReceiptDate = from.ExpectedReceiptDate,
            ReceivedQuantity = from.ReceivedQuantity,
            InvoicedQuantity = from.InvoicedQuantity
        });
    }
}
