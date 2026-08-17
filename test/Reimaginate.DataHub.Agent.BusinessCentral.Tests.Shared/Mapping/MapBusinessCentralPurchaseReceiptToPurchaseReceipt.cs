using Reimaginate.Mapper;
using BusinessCentralPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceipt;
using DataHubPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceipt;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseReceiptToPurchaseReceipt :
    ITypeMapper<BusinessCentralPurchaseReceipt, DataHubPurchaseReceipt>
{
    public Task<DataHubPurchaseReceipt> MapAsync(
        BusinessCentralPurchaseReceipt from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubPurchaseReceipt
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            ReceiptNumber = from.Number,
            InvoiceDate = from.InvoiceDate,
            PostingDate = from.PostingDate,
            DueDate = from.DueDate,
            SupplierNumber = from.VendorNumber,
            SupplierName = from.VendorName,
            PayToName = from.PayToName,
            PayToContact = from.PayToContact,
            ShipToName = from.ShipToName,
            ShipToContact = from.ShipToContact,
            ShipToCity = from.ShipToCity,
            ShipToCountry = from.ShipToCountry,
            ShipToState = from.ShipToState,
            ShipToPostCode = from.ShipToPostCode,
            CurrencyCode = from.CurrencyCode,
            PurchaseOrderNumber = from.OrderNumber
        });
}
