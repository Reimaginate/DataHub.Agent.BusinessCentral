using System.Security.Cryptography;
using System.Text;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using BusinessCentralVendorPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPayment;
using BusinessCentralVendorPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPaymentJournal;
using DataHubGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;
using DataHubVendorPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPayment;
using DataHubVendorPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPaymentJournal;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapVendorPaymentJournalToBusinessCentralVendorPaymentJournal :
    ITypeMapper<DataHubVendorPaymentJournal, BusinessCentralVendorPaymentJournal>,
    IDataHubTypeMapper<DataHubVendorPaymentJournal, BusinessCentralVendorPaymentJournal>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DataHubVendorPaymentJournal.BalancingAccount)];

    public Task<BusinessCentralVendorPaymentJournal> MapAsync(DataHubVendorPaymentJournal from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        Guid? balancingId = null;
        if (from.BalancingAccount is not null)
        {
            balancingId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubGeneralLedgerAccount>(
                from.BalancingAccount, typeof(BusinessCentralGeneralLedgerAccount).Name, cache);
            if (!balancingId.HasValue) throw new InvalidOperationException($"Vendor payment journal '{from.id}' requires a tracked G/L balancing account.");
        }

        var code = from.Code;
        var tracked = from.alternateKeys?.Any(key => key.Key.Equals("businesscentral.vendorpaymentjournal", StringComparison.OrdinalIgnoreCase)) == true;
        if (string.IsNullOrWhiteSpace(code) && !tracked) code = StableCode("DV", from.id, 8);
        return Task.FromResult(new BusinessCentralVendorPaymentJournal
        {
            Code = code,
            DisplayName = from.DisplayName,
            BalancingAccountId = balancingId
        });
    }

    private static string StableCode(string prefix, string id, int length) =>
        prefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)))[..length];
}

public sealed class MapBusinessCentralVendorPaymentJournalToVendorPaymentJournal :
    ITypeMapper<BusinessCentralVendorPaymentJournal, DataHubVendorPaymentJournal>
{
    public Task<DataHubVendorPaymentJournal> MapAsync(BusinessCentralVendorPaymentJournal from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubVendorPaymentJournal
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code, DisplayName = from.DisplayName,
            BalancingAccount = BusinessCentralMappingHelpers.ToDataHubReference<DataHubGeneralLedgerAccount, BusinessCentralGeneralLedgerAccount>(from.BalancingAccountId),
            BalancingAccountNumber = from.BalancingAccountNumber
        });
}

public sealed class MapVendorPaymentToBusinessCentralVendorPayment :
    ITypeMapper<DataHubVendorPayment, BusinessCentralVendorPayment>,
    IDataHubTypeMapper<DataHubVendorPayment, BusinessCentralVendorPayment>
{
    public List<string> MappedEntityReferences { get; } =
        [nameof(DataHubVendorPayment.PaymentJournal), nameof(DataHubVendorPayment.Supplier), nameof(DataHubVendorPayment.AppliesToPurchaseInvoice)];

    public Task<BusinessCentralVendorPayment> MapAsync(DataHubVendorPayment from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        if (from.PaymentJournal is null || from.Supplier is null)
            throw new InvalidOperationException("A Data Hub vendor payment must reference both a payment journal and a supplier before it can be synced.");
        var journalId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubVendorPaymentJournal>(from.PaymentJournal, typeof(BusinessCentralVendorPaymentJournal).Name, cache);
        var vendorId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubSupplier>(from.Supplier, typeof(BusinessCentralVendor).Name, cache);
        if (!journalId.HasValue || !vendorId.HasValue)
            throw new InvalidOperationException($"Vendor payment '{from.id}' requires tracked Business Central journal and vendor ids.");

        Guid? invoiceId = null;
        if (from.AppliesToPurchaseInvoice is not null)
        {
            invoiceId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubPurchaseInvoice>(from.AppliesToPurchaseInvoice, typeof(BusinessCentralPurchaseInvoice).Name, cache);
            if (!invoiceId.HasValue) throw new InvalidOperationException($"The purchase invoice referenced by vendor payment '{from.id}' has no Business Central invoice id.");
        }

        var documentNumber = from.DocumentNumber;
        var tracked = from.alternateKeys?.Any(key => key.Key.Equals("businesscentral.vendorpayment", StringComparison.OrdinalIgnoreCase)) == true;
        if (string.IsNullOrWhiteSpace(documentNumber) && !tracked)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(from.id)));
            documentNumber = $"DH-VPAY-{hash[..12]}";
        }

        var result = new BusinessCentralVendorPayment
        {
            JournalId = journalId, VendorId = vendorId, PostingDate = from.PostingDate,
            DocumentNumber = documentNumber, ExternalDocumentNumber = from.ExternalDocumentNumber,
            Amount = from.Amount, Description = from.Description, Comment = from.Comment
        };
        if (invoiceId.HasValue) result.AppliesToInvoiceId = invoiceId;
        return Task.FromResult(result);
    }
}

public sealed class MapBusinessCentralVendorPaymentToVendorPayment : ITypeMapper<BusinessCentralVendorPayment, DataHubVendorPayment>
{
    public Task<DataHubVendorPayment> MapAsync(BusinessCentralVendorPayment from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubVendorPayment
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            PaymentJournal = BusinessCentralMappingHelpers.ToDataHubReference<DataHubVendorPaymentJournal, BusinessCentralVendorPaymentJournal>(from.JournalId),
            Supplier = BusinessCentralMappingHelpers.ToDataHubReference<DataHubSupplier, BusinessCentralVendor>(from.VendorId),
            AppliesToPurchaseInvoice = BusinessCentralMappingHelpers.ToDataHubReference<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>(from.AppliesToInvoiceId),
            LineNumber = from.LineNumber, PostingDate = from.PostingDate, DocumentNumber = from.DocumentNumber,
            ExternalDocumentNumber = from.ExternalDocumentNumber, Amount = from.Amount,
            Description = from.Description, Comment = from.Comment
        });
}
