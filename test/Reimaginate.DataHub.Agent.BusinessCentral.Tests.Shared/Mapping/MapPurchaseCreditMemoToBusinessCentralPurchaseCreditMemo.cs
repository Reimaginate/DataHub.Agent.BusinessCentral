using System.Security.Cryptography;
using System.Text;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemo;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapPurchaseCreditMemoToBusinessCentralPurchaseCreditMemo :
    ITypeMapper<DataHubPurchaseCreditMemo, BusinessCentralPurchaseCreditMemo>,
    IDataHubTypeMapper<DataHubPurchaseCreditMemo, BusinessCentralPurchaseCreditMemo>
{
    public List<string> MappedEntityReferences { get; } =
    [
        nameof(DataHubPurchaseCreditMemo.Supplier),
        nameof(DataHubPurchaseCreditMemo.OriginalPurchaseInvoice)
    ];

    public Task<BusinessCentralPurchaseCreditMemo> MapAsync(
        DataHubPurchaseCreditMemo from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Supplier is null)
        {
            throw new InvalidOperationException(
                "A Data Hub purchase credit memo must reference a supplier before it can be synced.");
        }

        var vendorId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSupplier>(
            from.Supplier,
            typeof(BusinessCentralVendor).Name,
            cache);
        if (!vendorId.HasValue)
        {
            throw new InvalidOperationException(
                $"The supplier referenced by purchase credit memo '{from.id}' has no Business Central vendor id.");
        }

        Guid? invoiceId = null;
        if (from.OriginalPurchaseInvoice is not null)
        {
            invoiceId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubPurchaseInvoice>(
                from.OriginalPurchaseInvoice,
                typeof(BusinessCentralPurchaseInvoice).Name,
                cache);
            if (!invoiceId.HasValue)
            {
                throw new InvalidOperationException(
                    $"The original purchase invoice referenced by purchase credit memo '{from.id}' has no Business Central purchase invoice id.");
            }
        }

        var tracked = from.alternateKeys?.Any(key =>
            key.Key.Equals("businesscentral.purchasecreditmemo", StringComparison.OrdinalIgnoreCase)) == true;
        var vendorCreditMemoNumber = from.VendorCreditMemoNumber;
        if (string.IsNullOrWhiteSpace(vendorCreditMemoNumber) && !tracked)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(from.id)));
            vendorCreditMemoNumber = $"DHIT-PCM-{hash[..26]}";
        }

        var result = new BusinessCentralPurchaseCreditMemo
        {
            VendorId = vendorId,
            VendorCreditMemoNumber = vendorCreditMemoNumber,
            CreditMemoDate = from.CreditMemoDate,
            PostingDate = from.PostingDate,
            DiscountAmount = from.DiscountAmount ?? 0m
        };

        if (invoiceId.HasValue)
        {
            result.InvoiceId = invoiceId;
        }

        return Task.FromResult(result);
    }
}
