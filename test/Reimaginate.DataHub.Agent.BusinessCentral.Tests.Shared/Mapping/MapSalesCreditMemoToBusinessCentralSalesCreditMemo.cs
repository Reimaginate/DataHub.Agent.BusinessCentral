using System.Security.Cryptography;
using System.Text;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemo;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSalesCreditMemoToBusinessCentralSalesCreditMemo :
    ITypeMapper<DataHubSalesCreditMemo, BusinessCentralSalesCreditMemo>,
    IDataHubTypeMapper<DataHubSalesCreditMemo, BusinessCentralSalesCreditMemo>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubSalesCreditMemo.Customer),
        nameof(DataHubSalesCreditMemo.OriginalSalesInvoice)
    ];

    public Task<BusinessCentralSalesCreditMemo> MapAsync(
        DataHubSalesCreditMemo from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Customer is null)
        {
            throw new InvalidOperationException(
                "A Data Hub sales credit memo must reference a customer account before it can be synced.");
        }

        var customerId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubAccount>(
            from.Customer,
            typeof(BusinessCentralCustomer).Name,
            cache);
        if (!customerId.HasValue)
        {
            throw new InvalidOperationException(
                $"The customer account referenced by sales credit memo '{from.id}' has no Business Central customer id.");
        }

        Guid? invoiceId = null;
        if (from.OriginalSalesInvoice is not null)
        {
            invoiceId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSalesInvoice>(
                from.OriginalSalesInvoice,
                typeof(BusinessCentralSalesInvoice).Name,
                cache);
            if (!invoiceId.HasValue)
            {
                throw new InvalidOperationException(
                    $"The original sales invoice referenced by sales credit memo '{from.id}' has no Business Central sales invoice id.");
            }
        }

        var isTrackedInBusinessCentral = from.alternateKeys?.Any(key =>
            key.Key.Equals("businesscentral.salescreditmemo", StringComparison.OrdinalIgnoreCase)) == true;
        var externalDocumentNumber = from.ExternalDocumentNumber;
        if (string.IsNullOrWhiteSpace(externalDocumentNumber) && !isTrackedInBusinessCentral)
        {
            externalDocumentNumber = CreateReconciliationExternalDocumentNumber(from.id);
        }

        var result = new BusinessCentralSalesCreditMemo
        {
            ExternalDocumentNumber = externalDocumentNumber,
            CreditMemoDate = from.CreditMemoDate,
            PostingDate = from.PostingDate,
            CustomerId = customerId,
            PhoneNumber = from.PhoneNumber,
            Email = from.Email,
            // Business Central normalizes an omitted draft-credit-memo discount to zero.
            // Sending the normalized value prevents a perpetual null-versus-zero update.
            DiscountAmount = from.DiscountAmount ?? 0m
        };

        // The invoice association is optional. Omitting it from the change state when no
        // Data Hub reference is supplied avoids clearing a Business Central-owned snapshot.
        if (invoiceId.HasValue)
        {
            result.InvoiceId = invoiceId;
        }

        return Task.FromResult(result);
    }

    private static string CreateReconciliationExternalDocumentNumber(string dataHubId)
    {
        // External Document No. is limited to 35 characters. Use the complete Data Hub id
        // as the hash input so long shared prefixes do not make test records collide.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataHubId)));
        return $"DHIT-CM-{hash[..27]}";
    }
}
