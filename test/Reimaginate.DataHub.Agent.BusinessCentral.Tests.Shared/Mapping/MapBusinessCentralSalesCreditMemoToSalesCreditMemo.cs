using Reimaginate.Mapper;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemo;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralSalesCreditMemoToSalesCreditMemo :
    ITypeMapper<BusinessCentralSalesCreditMemo, DataHubSalesCreditMemo>
{
    public Task<DataHubSalesCreditMemo> MapAsync(
        BusinessCentralSalesCreditMemo from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSalesCreditMemo
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            CreditMemoNumber = from.Number,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            CreditMemoDate = from.CreditMemoDate,
            PostingDate = from.PostingDate,
            DueDate = from.DueDate,
            Customer = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubAccount, BusinessCentralCustomer>(from.CustomerId),
            OriginalSalesInvoice = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSalesInvoice, BusinessCentralSalesInvoice>(from.InvoiceId),
            PhoneNumber = from.PhoneNumber,
            Email = from.Email,
            Status = from.Status,
            PricesIncludeTax = from.PricesIncludeTax,
            DiscountAmount = from.DiscountAmount,
            DiscountAppliedBeforeTax = from.DiscountAppliedBeforeTax,
            TotalAmountExcludingTax = from.TotalAmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            TotalAmountIncludingTax = from.TotalAmountIncludingTax
        });
    }
}
