using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipment;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipment;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesShipmentToSalesShipment :
    ITypeMapper<BusinessCentralSalesShipment, DataHubSalesShipment>
{
    public Task<DataHubSalesShipment> MapAsync(
        BusinessCentralSalesShipment from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubSalesShipment
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            ShipmentNumber = from.Number,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            InvoiceDate = from.InvoiceDate,
            PostingDate = from.PostingDate,
            DueDate = from.DueDate,
            CustomerPurchaseOrderReference = from.CustomerPurchaseOrderReference,
            Customer = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubAccount, BusinessCentralCustomer>(from.CustomerId),
            CustomerNumber = from.CustomerNumber,
            CustomerName = from.CustomerName,
            ShipToName = from.ShipToName,
            ShipToContact = from.ShipToContact,
            ShipToCity = from.ShipToCity,
            ShipToCountry = from.ShipToCountry,
            ShipToState = from.ShipToState,
            ShipToPostCode = from.ShipToPostCode,
            CurrencyCode = from.CurrencyCode,
            SalesOrderNumber = from.OrderNumber,
            PricesIncludeTax = from.PricesIncludeTax,
            PhoneNumber = from.PhoneNumber,
            Email = from.Email
        });
}
