using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Xunit;
using BusinessCentralPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceipt;
using BusinessCentralPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceiptLine;
using BusinessCentralSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipment;
using BusinessCentralSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipmentLine;
using DataHubPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceipt;
using DataHubPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceiptLine;
using DataHubSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipment;
using DataHubSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipmentLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class PostedTransactionMappingTests
{
    [Theory]
    [InlineData(typeof(BusinessCentralSalesShipment), "salesShipments")]
    [InlineData(typeof(BusinessCentralSalesShipmentLine), "salesShipmentLines")]
    [InlineData(typeof(BusinessCentralPurchaseReceipt), "purchaseReceipts")]
    [InlineData(typeof(BusinessCentralPurchaseReceiptLine), "purchaseReceiptLines")]
    public void ModelsUseStandardV2Routes(Type type, string expected)
    {
        var route = Assert.Single(type.GetCustomAttributes(typeof(BusinessCentralUrlAttribute), true)
            .Cast<BusinessCentralUrlAttribute>());
        Assert.Equal(expected, route.Url);
    }

    [Fact]
    public async Task SalesShipmentHeaderAndLineMapAsReadOnlySnapshots()
    {
        var shipmentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var header = await new MapBusinessCentralSalesShipmentToSalesShipment().MapAsync(
            new BusinessCentralSalesShipment
            {
                Id = shipmentId.ToString(),
                Number = "SHIP-1",
                CustomerId = customerId,
                CustomerNumber = "C100",
                CustomerName = "Adatum",
                PostingDate = "2026-08-15",
                OrderNumber = "SO-1",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }, CancellationToken.None);
        var line = await new MapBusinessCentralSalesShipmentLineToSalesShipmentLine().MapAsync(
            new BusinessCentralSalesShipmentLine
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = shipmentId,
                DocumentNumber = "SHIP-1",
                Sequence = 10000,
                LineType = "Item",
                LineObjectNumber = "ITEM-1",
                Quantity = 3m,
                UnitPrice = 12.5m,
                ShipmentDate = "2026-08-15"
            }, CancellationToken.None);

        Assert.Equal("SHIP-1", header.ShipmentNumber);
        Assert.Equal("SO-1", header.SalesOrderNumber);
        Assert.IsType<Reimaginate.DataHub.SharedModels.Core.ExternalEntityReference>(header.Customer);
        Assert.Equal("ITEM-1", line.ProductNumber);
        Assert.Equal(3m, line.Quantity);
        Assert.IsType<Reimaginate.DataHub.SharedModels.Core.ExternalEntityReference>(line.SalesShipment);
    }

    [Fact]
    public async Task PurchaseReceiptHeaderAndLineMapNumberBasedSnapshots()
    {
        var receiptId = Guid.NewGuid();
        var header = await new MapBusinessCentralPurchaseReceiptToPurchaseReceipt().MapAsync(
            new BusinessCentralPurchaseReceipt
            {
                Id = receiptId.ToString(),
                Number = "RCPT-1",
                VendorNumber = "V100",
                VendorName = "Fabrikam",
                PostingDate = "2026-08-15",
                OrderNumber = "PO-1",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }, CancellationToken.None);
        var line = await new MapBusinessCentralPurchaseReceiptLineToPurchaseReceiptLine().MapAsync(
            new BusinessCentralPurchaseReceiptLine
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = receiptId,
                Sequence = 10000,
                LineType = "Item",
                LineObjectNumber = "ITEM-1",
                Quantity = 4m,
                UnitCost = 9.5m,
                ExpectedReceiptDate = "2026-08-15"
            }, CancellationToken.None);

        Assert.Equal("RCPT-1", header.ReceiptNumber);
        Assert.Equal("V100", header.SupplierNumber);
        Assert.Equal("PO-1", header.PurchaseOrderNumber);
        Assert.Equal("ITEM-1", line.ProductNumber);
        Assert.Equal(4m, line.Quantity);
        Assert.IsType<Reimaginate.DataHub.SharedModels.Core.ExternalEntityReference>(line.PurchaseReceipt);
    }

    [Fact]
    public async Task PostedTransactionsFailClearlyWhenMappedOutbound()
    {
        var failures = new[]
        {
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                new MapSalesShipmentToBusinessCentralSalesShipment().MapAsync(new DataHubSalesShipment(), CancellationToken.None)),
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                new MapSalesShipmentLineToBusinessCentralSalesShipmentLine().MapAsync(new DataHubSalesShipmentLine(), CancellationToken.None)),
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                new MapPurchaseReceiptToBusinessCentralPurchaseReceipt().MapAsync(new DataHubPurchaseReceipt(), CancellationToken.None)),
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                new MapPurchaseReceiptLineToBusinessCentralPurchaseReceiptLine().MapAsync(new DataHubPurchaseReceiptLine(), CancellationToken.None))
        };

        Assert.All(failures, failure => Assert.Contains("read-only", failure.Message, StringComparison.OrdinalIgnoreCase));
    }
}
