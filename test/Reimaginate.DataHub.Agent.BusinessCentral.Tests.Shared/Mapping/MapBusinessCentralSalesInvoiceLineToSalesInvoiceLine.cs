using Reimaginate.Mapper;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;
using DataHubSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesInvoiceLineToSalesInvoiceLine :
    ITypeMapper<BusinessCentralSalesInvoiceLine, DataHubSalesInvoiceLine>
{
    public Task<DataHubSalesInvoiceLine> MapAsync(
        BusinessCentralSalesInvoiceLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSalesInvoiceLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            SalesInvoice = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSalesInvoice, BusinessCentralSalesInvoice>(from.DocumentId),
            Product = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubProduct, BusinessCentralItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Description2 = from.Description2,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice,
            DiscountAmount = from.DiscountAmount,
            DiscountPercent = from.DiscountPercent,
            AmountExcludingTax = from.AmountExcludingTax,
            TaxPercent = from.TaxPercent,
            TotalTaxAmount = from.TotalTaxAmount,
            AmountIncludingTax = from.AmountIncludingTax
        });
    }
}
