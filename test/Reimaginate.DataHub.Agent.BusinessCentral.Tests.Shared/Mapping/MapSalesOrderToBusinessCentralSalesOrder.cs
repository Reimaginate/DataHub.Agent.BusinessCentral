using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrder;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrder;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSalesOrderToBusinessCentralSalesOrder :
    ITypeMapper<DataHubSalesOrder, BusinessCentralSalesOrder>,
    IDataHubTypeMapper<DataHubSalesOrder, BusinessCentralSalesOrder>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DataHubSalesOrder.Customer)];

    public Task<BusinessCentralSalesOrder> MapAsync(
        DataHubSalesOrder from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Customer is null)
        {
            throw new InvalidOperationException(
                "A Data Hub sales order must reference a customer account before it can be synced.");
        }

        var customerId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubAccount>(
            from.Customer,
            typeof(BusinessCentralCustomer).Name,
            cache);
        if (!customerId.HasValue)
        {
            throw new InvalidOperationException(
                $"The customer account referenced by sales order '{from.id}' has no Business Central customer id.");
        }

        return Task.FromResult(new BusinessCentralSalesOrder
        {
            DataHubCorrelationId = BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrder), from.id),
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            OrderDate = from.OrderDate,
            CustomerId = customerId,
            PhoneNumber = from.PhoneNumber,
            Email = from.Email
        });
    }
}
