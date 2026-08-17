using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrder;
using BusinessCentralSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrderLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrder;
using DataHubSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesOrderLineToSalesOrderLine :
    ITypeMapper<BusinessCentralSalesOrderLine, DataHubSalesOrderLine>
{
    public Task<DataHubSalesOrderLine> MapAsync(
        BusinessCentralSalesOrderLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSalesOrderLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            SalesOrder = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSalesOrder, BusinessCentralSalesOrder>(from.DocumentId),
            Product = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubProduct, BusinessCentralItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice,
            DiscountAmount = from.DiscountAmount,
            DiscountPercent = from.DiscountPercent,
            AmountExcludingTax = from.AmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            AmountIncludingTax = from.AmountIncludingTax
        });
    }
}
