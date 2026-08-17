using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemo;
using DataHubSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemoLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesCreditMemoLineToSalesCreditMemoLine :
    ITypeMapper<BusinessCentralSalesCreditMemoLine, DataHubSalesCreditMemoLine>
{
    public Task<DataHubSalesCreditMemoLine> MapAsync(
        BusinessCentralSalesCreditMemoLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSalesCreditMemoLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            SalesCreditMemo = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSalesCreditMemo, BusinessCentralSalesCreditMemo>(from.DocumentId),
            Product = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubProduct, BusinessCentralItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice,
            ManualDiscountAmount = from.DiscountAmount,
            DiscountPercent = from.DiscountPercent,
            AmountExcludingTax = from.AmountExcludingTax,
            TaxPercent = from.TaxPercent,
            TotalTaxAmount = from.TotalTaxAmount,
            AmountIncludingTax = from.AmountIncludingTax,
            ShipmentDate = from.ShipmentDate
        });
    }
}
