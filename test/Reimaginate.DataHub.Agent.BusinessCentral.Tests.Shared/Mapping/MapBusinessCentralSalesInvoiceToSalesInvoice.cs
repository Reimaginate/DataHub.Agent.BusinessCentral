using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesInvoiceToSalesInvoice :
    ITypeMapper<BusinessCentralSalesInvoice, DataHubSalesInvoice>
{
    public Task<DataHubSalesInvoice> MapAsync(
        BusinessCentralSalesInvoice from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSalesInvoice
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            InvoiceNumber = from.Number,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            InvoiceDate = from.InvoiceDate,
            PostingDate = from.PostingDate,
            DueDate = from.DueDate,
            Customer = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubAccount, BusinessCentralCustomer>(from.CustomerId),
            PhoneNumber = from.PhoneNumber,
            Email = from.Email,
            Status = from.Status,
            PricesIncludeTax = from.PricesIncludeTax,
            RemainingAmount = from.RemainingAmount,
            DiscountAmount = from.DiscountAmount,
            TotalAmountExcludingTax = from.TotalAmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            TotalAmountIncludingTax = from.TotalAmountIncludingTax
        });
    }
}
