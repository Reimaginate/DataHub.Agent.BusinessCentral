using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrder;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrder;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesOrderToSalesOrder :
    ITypeMapper<BusinessCentralSalesOrder, DataHubSalesOrder>
{
    public Task<DataHubSalesOrder> MapAsync(
        BusinessCentralSalesOrder from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSalesOrder
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            OrderNumber = from.Number,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            OrderDate = from.OrderDate,
            Customer = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubAccount, BusinessCentralCustomer>(from.CustomerId),
            PhoneNumber = from.PhoneNumber,
            Email = from.Email,
            Status = from.Status,
            TotalAmountExcludingTax = from.TotalAmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            TotalAmountIncludingTax = from.TotalAmountIncludingTax
        });
    }
}
