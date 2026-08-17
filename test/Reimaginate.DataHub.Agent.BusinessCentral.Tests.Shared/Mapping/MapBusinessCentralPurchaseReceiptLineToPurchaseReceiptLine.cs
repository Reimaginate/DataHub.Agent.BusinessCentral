using Reimaginate.Mapper;
using BusinessCentralPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceipt;
using BusinessCentralPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceiptLine;
using DataHubPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceipt;
using DataHubPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceiptLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseReceiptLineToPurchaseReceiptLine :
    ITypeMapper<BusinessCentralPurchaseReceiptLine, DataHubPurchaseReceiptLine>
{
    public Task<DataHubPurchaseReceiptLine> MapAsync(
        BusinessCentralPurchaseReceiptLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubPurchaseReceiptLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            PurchaseReceipt = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubPurchaseReceipt, BusinessCentralPurchaseReceipt>(from.DocumentId),
            Sequence = from.Sequence,
            LineType = from.LineType,
            ProductNumber = from.LineObjectNumber,
            Description = from.Description,
            Description2 = from.Description2,
            UnitOfMeasureCode = from.UnitOfMeasureCode,
            UnitCost = from.UnitCost,
            Quantity = from.Quantity,
            DiscountPercent = from.DiscountPercent,
            TaxPercent = from.TaxPercent,
            ExpectedReceiptDate = from.ExpectedReceiptDate
        });
}
