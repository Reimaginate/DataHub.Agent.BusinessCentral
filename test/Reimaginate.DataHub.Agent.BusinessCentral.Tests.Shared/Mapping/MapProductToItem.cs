using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapProductToItem :
    ITypeMapper<DataHubProduct, BusinessCentralItem>,
    IDataHubTypeMapper<DataHubProduct, BusinessCentralItem>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BusinessCentralItem> MapAsync(
        DataHubProduct from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (string.IsNullOrWhiteSpace(from.Name))
        {
            throw new InvalidOperationException(
                "A Data Hub product must have a name before it can be synced as a Business Central item.");
        }

        var item = new BusinessCentralItem
        {
            Number = string.IsNullOrWhiteSpace(from.ProductNumber)
                ? CreateItemNumber(from.id)
                : from.ProductNumber,
            DisplayName = from.Name,
            DisplayName2 = from.Description,
            UnitPrice = from.Price
        };

        if (from.alternateKeys?.Any(key =>
                key.Key.Equals("businesscentral.item", StringComparison.OrdinalIgnoreCase)) != true)
        {
            item.Type = "Service";
        }

        return Task.FromResult(item);
    }

    private static string CreateItemNumber(string productId)
    {
        var suffix = new string(productId.Where(char.IsLetterOrDigit).Take(15).ToArray());
        return $"DHIT-{suffix}".ToUpperInvariant();
    }
}
