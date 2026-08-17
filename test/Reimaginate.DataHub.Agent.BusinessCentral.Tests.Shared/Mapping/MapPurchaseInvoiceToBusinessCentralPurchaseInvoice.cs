using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapPurchaseInvoiceToBusinessCentralPurchaseInvoice :
    ITypeMapper<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>,
    IDataHubTypeMapper<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DataHubPurchaseInvoice.Supplier)];

    public Task<BusinessCentralPurchaseInvoice> MapAsync(
        DataHubPurchaseInvoice from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Supplier is null)
        {
            throw new InvalidOperationException(
                "A Data Hub purchase invoice must reference a supplier before it can be synced.");
        }

        var vendorId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSupplier>(
            from.Supplier,
            typeof(BusinessCentralVendor).Name,
            cache);
        if (!vendorId.HasValue)
        {
            throw new InvalidOperationException(
                $"The supplier referenced by purchase invoice '{from.id}' has no Business Central vendor id.");
        }

        return Task.FromResult(new BusinessCentralPurchaseInvoice
        {
            VendorInvoiceNumber = from.VendorInvoiceNumber,
            PostingDate = from.PostingDate,
            InvoiceDate = from.InvoiceDate,
            DueDate = from.DueDate,
            VendorId = vendorId
        });
    }
}
