using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuote;
using BusinessCentralSalesQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuoteLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Quote;
using DataHubQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.QuoteLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesQuoteLineToQuoteLine :
    ITypeMapper<BusinessCentralSalesQuoteLine, DataHubQuoteLine>
{
    public Task<DataHubQuoteLine> MapAsync(
        BusinessCentralSalesQuoteLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubQuoteLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            Quote = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubQuote, BusinessCentralSalesQuote>(from.DocumentId),
            Product = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubProduct, BusinessCentralItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            PricePerUnit = from.UnitPrice,
            ManualDiscountAmount = from.DiscountAmount,
            DiscountPercent = from.DiscountPercent,
            BaseAmount = from.AmountExcludingTax,
            TaxPercent = from.TaxPercent,
            TotalTaxAmount = from.TotalTaxAmount,
            ExtendedAmount = from.AmountIncludingTax
        });
    }
}
