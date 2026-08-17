using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.UnitOfMeasure;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.UnitOfMeasure;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapItemToProduct : ITypeMapper<BusinessCentralItem, DataHubProduct>
{
    public Task<DataHubProduct> MapAsync(
        BusinessCentralItem from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubProduct
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            ProductNumber = from.Number,
            Name = from.DisplayName,
            Description = from.DisplayName2,
            Price = from.UnitPrice,
            BaseUnitOfMeasure = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubUnitOfMeasure, BusinessCentralUnitOfMeasure>(from.BaseUnitOfMeasureId),
            BaseUnitOfMeasureCode = from.BaseUnitOfMeasureCode
        });
    }
}
