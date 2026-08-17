using Reimaginate.Mapper;
using BusinessCentralSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipment;
using BusinessCentralSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipmentLine;
using DataHubSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipment;
using DataHubSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipmentLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesShipmentLineToSalesShipmentLine :
    ITypeMapper<BusinessCentralSalesShipmentLine, DataHubSalesShipmentLine>
{
    public Task<DataHubSalesShipmentLine> MapAsync(
        BusinessCentralSalesShipmentLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubSalesShipmentLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            SalesShipment = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSalesShipment, BusinessCentralSalesShipment>(from.DocumentId),
            DocumentNumber = from.DocumentNumber,
            Sequence = from.Sequence,
            LineType = from.LineType,
            ProductNumber = from.LineObjectNumber,
            Description = from.Description,
            Description2 = from.Description2,
            UnitOfMeasureCode = from.UnitOfMeasureCode,
            UnitPrice = from.UnitPrice,
            Quantity = from.Quantity,
            DiscountPercent = from.DiscountPercent,
            TaxPercent = from.TaxPercent,
            ShipmentDate = from.ShipmentDate
        });
}
