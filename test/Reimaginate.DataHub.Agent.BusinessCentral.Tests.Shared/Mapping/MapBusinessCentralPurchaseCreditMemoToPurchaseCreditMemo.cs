using Reimaginate.Mapper;
using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemo;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralPurchaseCreditMemoToPurchaseCreditMemo :
    ITypeMapper<BusinessCentralPurchaseCreditMemo, DataHubPurchaseCreditMemo>
{
    public Task<DataHubPurchaseCreditMemo> MapAsync(
        BusinessCentralPurchaseCreditMemo from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubPurchaseCreditMemo
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            CreditMemoNumber = from.Number,
            VendorCreditMemoNumber = from.VendorCreditMemoNumber,
            CreditMemoDate = from.CreditMemoDate,
            PostingDate = from.PostingDate,
            DueDate = from.DueDate,
            Supplier = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubSupplier, BusinessCentralVendor>(from.VendorId),
            OriginalPurchaseInvoice = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>(from.InvoiceId),
            Status = from.Status,
            PricesIncludeTax = from.PricesIncludeTax,
            DiscountAmount = from.DiscountAmount,
            DiscountAppliedBeforeTax = from.DiscountAppliedBeforeTax,
            TotalAmountExcludingTax = from.TotalAmountExcludingTax,
            TotalTaxAmount = from.TotalTaxAmount,
            TotalAmountIncludingTax = from.TotalAmountIncludingTax
        });
}
