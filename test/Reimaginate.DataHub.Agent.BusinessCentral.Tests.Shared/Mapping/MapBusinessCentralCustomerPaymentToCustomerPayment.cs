using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPayment;
using BusinessCentralCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentJournal;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPayment;
using DataHubCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentJournal;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralCustomerPaymentToCustomerPayment :
    ITypeMapper<BusinessCentralCustomerPayment, DataHubCustomerPayment>
{
    public Task<DataHubCustomerPayment> MapAsync(
        BusinessCentralCustomerPayment from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubCustomerPayment
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            PaymentJournal = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubCustomerPaymentJournal, BusinessCentralCustomerPaymentJournal>(from.JournalId),
            Customer = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubAccount, BusinessCentralCustomer>(from.CustomerId),
            AppliesToSalesInvoice = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSalesInvoice, BusinessCentralSalesInvoice>(from.AppliesToInvoiceId),
            LineNumber = from.LineNumber,
            PostingDate = from.PostingDate,
            DocumentNumber = from.DocumentNumber,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            Amount = from.Amount,
            Description = from.Description,
            Comment = from.Comment
        });
}
