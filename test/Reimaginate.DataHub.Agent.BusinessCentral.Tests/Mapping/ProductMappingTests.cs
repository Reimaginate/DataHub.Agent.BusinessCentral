using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class ProductMappingTests
{
    [Fact]
    public async Task NewDataHubProductMapsOnlyOwnedFieldsAndCreatesAServiceItem()
    {
        var source = NewDataHubProduct();

        var result = await new MapProductToItem().MapAsync(source, CancellationToken.None);

        Assert.Equal("DHIT-0123456789ABCDE", result.Number);
        Assert.Equal(source.Name, result.DisplayName);
        Assert.Equal(source.Description, result.DisplayName2);
        Assert.Equal(source.Price, result.UnitPrice);
        Assert.Equal("Service", result.Type);
        Assert.Null(result.Blocked);
        Assert.Null(result.UnitCost);
        Assert.Null(result.Inventory);
        Assert.Null(result.Gtin);
        Assert.Null(result.BaseUnitOfMeasureCode);
        Assert.Null(result.GeneralProductPostingGroupCode);
        Assert.Null(result.InventoryPostingGroupCode);
    }

    [Fact]
    public async Task ExistingDataHubProductDoesNotOverwriteBusinessCentralOwnedItemType()
    {
        var source = NewDataHubProduct();
        source.alternateKeys =
        [
            new AlternateKey
            {
                Key = "businesscentral.item",
                Value = Guid.NewGuid().ToString()
            }
        ];

        var result = await new MapProductToItem().MapAsync(source, CancellationToken.None);

        Assert.Null(result.Type);
        Assert.Equal(source.Name, result.DisplayName);
        Assert.Equal(source.Description, result.DisplayName2);
        Assert.Equal(source.Price, result.UnitPrice);
    }

    [Fact]
    public async Task BusinessCentralItemMapsOnlyDataHubProductFields()
    {
        var modified = new DateTimeOffset(2026, 8, 13, 1, 2, 3, TimeSpan.Zero);
        var source = new BusinessCentralItem
        {
            Id = Guid.NewGuid().ToString(),
            Number = "ITEM-100",
            DisplayName = "Consulting",
            DisplayName2 = "Professional services",
            UnitPrice = 250.75m,
            Type = "Service",
            Blocked = true,
            UnitCost = 100m,
            Inventory = 3m,
            LastModifiedDateTime = modified
        };

        var result = await new MapItemToProduct().MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(modified, result.createdOn);
        Assert.Equal(modified, result.lastUpdated);
        Assert.Equal(source.Number, result.ProductNumber);
        Assert.Equal(source.DisplayName, result.Name);
        Assert.Equal(source.DisplayName2, result.Description);
        Assert.Equal(source.UnitPrice, result.Price);
    }

    [Fact]
    public async Task DataHubProductRequiresAName()
    {
        var source = NewDataHubProduct();
        source.Name = null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapProductToItem().MapAsync(source, CancellationToken.None));

        Assert.Contains("must have a name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DataHubProduct NewDataHubProduct()
    {
        return new DataHubProduct
        {
            id = "0123456789abcdef0123456789abcdef",
            Name = "Consulting",
            Description = "Professional services",
            Price = 250.75m
        };
    }
}
