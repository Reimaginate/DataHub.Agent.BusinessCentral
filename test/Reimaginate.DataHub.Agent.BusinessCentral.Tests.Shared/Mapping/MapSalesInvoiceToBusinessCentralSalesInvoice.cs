using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSalesInvoiceToBusinessCentralSalesInvoice :
    ITypeMapper<DataHubSalesInvoice, BusinessCentralSalesInvoice>,
    IDataHubTypeMapper<DataHubSalesInvoice, BusinessCentralSalesInvoice>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DataHubSalesInvoice.Customer)];

    public Task<BusinessCentralSalesInvoice> MapAsync(
        DataHubSalesInvoice from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Customer is null)
        {
            throw new InvalidOperationException(
                "A Data Hub sales invoice must reference a customer account before it can be synced.");
        }

        var customerId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubAccount>(
            from.Customer,
            typeof(BusinessCentralCustomer).Name,
            cache);
        if (!customerId.HasValue)
        {
            throw new InvalidOperationException(
                $"The customer account referenced by sales invoice '{from.id}' has no Business Central customer id.");
        }

        return Task.FromResult(new BusinessCentralSalesInvoice
        {
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            InvoiceDate = from.InvoiceDate,
            PostingDate = from.PostingDate,
            DueDate = from.DueDate,
            CustomerId = customerId,
            PhoneNumber = from.PhoneNumber,
            Email = from.Email
        });
    }
}
