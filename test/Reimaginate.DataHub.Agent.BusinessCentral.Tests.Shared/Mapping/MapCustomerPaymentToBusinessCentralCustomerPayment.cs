using System.Security.Cryptography;
using System.Text;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
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

public sealed class MapCustomerPaymentToBusinessCentralCustomerPayment :
    ITypeMapper<DataHubCustomerPayment, BusinessCentralCustomerPayment>,
    IDataHubTypeMapper<DataHubCustomerPayment, BusinessCentralCustomerPayment>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubCustomerPayment.PaymentJournal),
        nameof(DataHubCustomerPayment.Customer),
        nameof(DataHubCustomerPayment.AppliesToSalesInvoice)
    ];

    public Task<BusinessCentralCustomerPayment> MapAsync(
        DataHubCustomerPayment from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.PaymentJournal is null || from.Customer is null)
        {
            throw new InvalidOperationException(
                "A Data Hub customer payment must reference both a payment journal and a customer before it can be synced.");
        }

        var journalId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubCustomerPaymentJournal>(
            from.PaymentJournal,
            typeof(BusinessCentralCustomerPaymentJournal).Name,
            cache);
        var customerId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubAccount>(
            from.Customer,
            typeof(BusinessCentralCustomer).Name,
            cache);
        if (!journalId.HasValue || !customerId.HasValue)
        {
            throw new InvalidOperationException(
                $"Customer payment '{from.id}' requires tracked Business Central journal and customer ids.");
        }

        Guid? invoiceId = null;
        if (from.AppliesToSalesInvoice is not null)
        {
            invoiceId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSalesInvoice>(
                from.AppliesToSalesInvoice,
                typeof(BusinessCentralSalesInvoice).Name,
                cache);
            if (!invoiceId.HasValue)
            {
                throw new InvalidOperationException(
                    $"The sales invoice referenced by customer payment '{from.id}' has no Business Central invoice id.");
            }
        }

        var documentNumber = from.DocumentNumber;
        var tracked = from.alternateKeys?.Any(key =>
            key.Key.Equals("businesscentral.customerpayment", StringComparison.OrdinalIgnoreCase)) == true;
        if (string.IsNullOrWhiteSpace(documentNumber) && !tracked)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(from.id)));
            documentNumber = $"DH-PAY-{hash[..13]}";
        }

        var result = new BusinessCentralCustomerPayment
        {
            JournalId = journalId,
            CustomerId = customerId,
            PostingDate = from.PostingDate,
            DocumentNumber = documentNumber,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            Amount = from.Amount,
            Description = from.Description,
            Comment = from.Comment
        };
        if (invoiceId.HasValue)
        {
            result.AppliesToInvoiceId = invoiceId;
        }

        return Task.FromResult(result);
    }
}
