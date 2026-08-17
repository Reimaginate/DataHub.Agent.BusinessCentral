using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemoLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemo;
using DataHubPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemoLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseCreditMemoLineToPurchaseCreditMemoLine :
    ITypeMapper<BusinessCentralPurchaseCreditMemoLine, DataHubPurchaseCreditMemoLine>
{
    public Task<DataHubPurchaseCreditMemoLine> MapAsync(
        BusinessCentralPurchaseCreditMemoLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubPurchaseCreditMemoLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            PurchaseCreditMemo = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubPurchaseCreditMemo, BusinessCentralPurchaseCreditMemo>(from.DocumentId),
            Product = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubProduct, BusinessCentralItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Quantity = from.Quantity,
            UnitCost = from.UnitCost,
            DiscountAmount = from.DiscountAmount,
            DiscountPercent = from.DiscountPercent,
            DiscountAppliedBeforeTax = from.DiscountAppliedBeforeTax,
            AmountExcludingTax = from.AmountExcludingTax,
            TaxPercent = from.TaxPercent,
            TotalTaxAmount = from.TotalTaxAmount,
            AmountIncludingTax = from.AmountIncludingTax
        });
}
