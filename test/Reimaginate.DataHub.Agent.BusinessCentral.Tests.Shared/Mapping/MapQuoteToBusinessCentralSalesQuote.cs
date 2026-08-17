using System.Security.Cryptography;
using System.Text;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuote;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Quote;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapQuoteToBusinessCentralSalesQuote :
    ITypeMapper<DataHubQuote, BusinessCentralSalesQuote>,
    IDataHubTypeMapper<DataHubQuote, BusinessCentralSalesQuote>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DataHubQuote.CustomerAccount)];

    public Task<BusinessCentralSalesQuote> MapAsync(
        DataHubQuote from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.CustomerAccount is null)
        {
            throw new InvalidOperationException(
                "A Data Hub quote must reference a customer account before it can be synced.");
        }

        var customerId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubAccount>(
            from.CustomerAccount,
            typeof(BusinessCentralCustomer).Name,
            cache);
        if (!customerId.HasValue)
        {
            throw new InvalidOperationException(
                $"The customer account referenced by quote '{from.id}' has no Business Central customer id.");
        }

        var isTrackedInBusinessCentral = from.alternateKeys?.Any(key =>
            key.Key.Equals("businesscentral.salesquote", StringComparison.OrdinalIgnoreCase)) == true;
        var externalDocumentNumber = from.ExternalDocumentNumber;
        if (string.IsNullOrWhiteSpace(externalDocumentNumber) && !isTrackedInBusinessCentral)
        {
            externalDocumentNumber = CreateRecoveryExternalDocumentNumber(from.id);
        }

        return Task.FromResult(new BusinessCentralSalesQuote
        {
            ExternalDocumentNumber = externalDocumentNumber,
            DocumentDate = BusinessCentralMappingHelpers.ToBusinessCentralDate(from.EffectiveFrom),
            CustomerId = customerId,
            // Business Central normalizes an omitted draft-quote discount to zero. Sending
            // the same normalized value prevents null-versus-zero round trips from producing
            // a perpetual header update.
            DiscountAmount = from.DiscountAmount ?? 0m,
            ValidUntilDate = BusinessCentralMappingHelpers.ToBusinessCentralDate(from.EffectiveTo),
            PhoneNumber = from.PhoneNumber,
            Email = from.Email
        });
    }

    private static string CreateRecoveryExternalDocumentNumber(string dataHubId)
    {
        // Business Central limits External Document No. to 35 characters. Hashing the complete
        // Data Hub id avoids collisions between ids that share a long common prefix while
        // retaining a stable, test-identifiable value.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataHubId)));
        return $"DHIT-Q-{hash[..28]}";
    }
}
