using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuote;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Quote;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesQuoteToQuote :
    ITypeMapper<BusinessCentralSalesQuote, DataHubQuote>
{
    public Task<DataHubQuote> MapAsync(
        BusinessCentralSalesQuote from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubQuote
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Name = from.ExternalDocumentNumber ?? from.Number,
            QuoteNumber = from.Number,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            EffectiveFrom = BusinessCentralMappingHelpers.ToDataHubDate(from.DocumentDate),
            EffectiveTo = BusinessCentralMappingHelpers.ToDataHubDate(from.ValidUntilDate),
            CustomerAccount = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubAccount, BusinessCentralCustomer>(from.CustomerId),
            PhoneNumber = from.PhoneNumber,
            Email = from.Email,
            Status = from.Status,
            DiscountAmount = from.DiscountAmount,
            TotalLineItemAmount = from.TotalAmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            TotalAmount = from.TotalAmountIncludingTax
        });
    }
}
