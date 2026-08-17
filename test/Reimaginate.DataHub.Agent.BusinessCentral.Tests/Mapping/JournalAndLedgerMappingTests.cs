using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BCAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using BCBank = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.BankAccount;
using BCGeneralJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournal;
using BCGeneralLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalLine;
using BCGeneralDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalDimensionSetLine;
using BCLedger = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerEntry;
using BCItemLedger = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.ItemLedgerEntry;
using BCVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using BCVendorJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPaymentJournal;
using BCVendorPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPayment;
using BCVendorDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPaymentDimensionSetLine;
using DHAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;
using DHBank = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.BankAccount;
using DHGeneralJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournal;
using DHGeneralLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournalLine;
using DHGeneralDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournalLineDimension;
using DHLedger = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerEntry;
using DHItemLedger = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.ItemLedgerEntry;
using DHValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;
using DHSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;
using DHVendorJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPaymentJournal;
using DHVendorPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPayment;
using DHVendorDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPaymentDimension;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class JournalAndLedgerMappingTests
{
    [Fact]
    public void StandardRoutesAndParentsAreDeclared()
    {
        Assert.Equal("vendorPaymentJournals", Url<BCVendorJournal>());
        Assert.Equal("vendorPayments", Url<BCVendorPayment>());
        Assert.Equal("journals", Url<BCGeneralJournal>());
        Assert.Equal("journalLines", Url<BCGeneralLine>());
        Assert.Equal("generalLedgerEntries", Url<BCLedger>());
        Assert.Equal("itemLedgerEntries", Url<BCItemLedger>());
        AssertParent<BCVendorPayment>("vendorPaymentJournals", nameof(BCVendorPayment.JournalId));
        AssertParent<BCVendorDimension>("vendorPayments", nameof(BCVendorDimension.ParentId));
        AssertParent<BCGeneralLine>("journals", nameof(BCGeneralLine.JournalId));
        AssertParent<BCGeneralDimension>("journalLines", nameof(BCGeneralDimension.ParentId));
    }

    [Fact]
    public async Task VendorPaymentMapsJournalVendorAndValues()
    {
        var journalId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var source = new DHVendorPayment
        {
            id = "vendor-payment-1", PaymentJournal = Ref<DHVendorJournal>("journal-1"),
            Supplier = Ref<DHSupplier>("supplier-1"), PostingDate = "2026-08-15", Amount = 75m
        };
        var cache = Cache<DHVendorJournal>("journal-1", typeof(BCVendorJournal).Name, journalId);
        cache[typeof(DHSupplier).Name] = CacheObjects("supplier-1", typeof(BCVendor).Name, vendorId);

        var result = await new MapVendorPaymentToBusinessCentralVendorPayment().MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(journalId, result.JournalId);
        Assert.Equal(vendorId, result.VendorId);
        Assert.StartsWith("DH-VPAY-", result.DocumentNumber);
        Assert.Equal(75m, result.Amount);
    }

    [Fact]
    public async Task VendorPaymentInboundReferencesArePreserved()
    {
        var journalId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var result = await new MapBusinessCentralVendorPaymentToVendorPayment().MapAsync(
            new BCVendorPayment { Id = Guid.NewGuid().ToString(), JournalId = journalId, VendorId = vendorId, Amount = 10m },
            CancellationToken.None);

        AssertExternal<DHVendorJournal, BCVendorJournal>(result.PaymentJournal, journalId);
        AssertExternal<DHSupplier, BCVendor>(result.Supplier, vendorId);
        Assert.Equal(10m, result.Amount);
    }

    [Fact]
    public async Task GeneralJournalLineMapsGlAndBankReferences()
    {
        var journalId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var bankId = Guid.NewGuid();
        var source = new DHGeneralLine
        {
            id = "line-1", Journal = Ref<DHGeneralJournal>("journal-1"), Account = Ref<DHAccount>("account-1"),
            BalancingAccount = Ref<DHBank>("bank-1"), AccountType = "G/L Account",
            BalanceAccountType = "Bank Account", BalancingAccountNumber = "BANK-1",
            PostingDate = "2026-08-15", Amount = 20m
        };
        var cache = Cache<DHGeneralJournal>("journal-1", typeof(BCGeneralJournal).Name, journalId);
        cache[typeof(DHAccount).Name] = CacheObjects("account-1", typeof(BCAccount).Name, accountId);
        cache[typeof(DHBank).Name] = CacheObjects("bank-1", typeof(BCBank).Name, bankId);

        var result = await new MapGeneralJournalLineToBusinessCentralGeneralJournalLine().MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(journalId, result.JournalId);
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal("G/L Account", result.AccountType);
        Assert.Null(result.BalancingAccountId);
        Assert.Equal("BANK-1", result.BalancingAccountNumber);
        Assert.Equal("Bank Account", result.BalanceAccountType);
    }

    [Fact]
    public async Task GeneralJournalLineRequiresBankNumberForBankBalancingAccount()
    {
        var source = new DHGeneralLine
        {
            id = "line-bank-number-missing",
            Journal = Ref<DHGeneralJournal>("journal-1"),
            Account = Ref<DHAccount>("account-1"),
            BalancingAccount = Ref<DHBank>("bank-1"),
            AccountType = "G/L Account",
            BalanceAccountType = "Bank Account"
        };
        var cache = Cache<DHGeneralJournal>("journal-1", typeof(BCGeneralJournal).Name, Guid.NewGuid());
        cache[typeof(DHAccount).Name] = CacheObjects("account-1", typeof(BCAccount).Name, Guid.NewGuid());
        cache[typeof(DHBank).Name] = CacheObjects("bank-1", typeof(BCBank).Name, Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapGeneralJournalLineToBusinessCentralGeneralJournalLine().MapAsync(source, CancellationToken.None, cache));

        Assert.Contains(nameof(DHGeneralLine.BalancingAccountNumber), exception.Message);
    }

    [Fact]
    public async Task TrackedGeneralJournalLineOmitsUnchangedBankFieldsWhenPatchIsPartial()
    {
        var source = new DHGeneralLine
        {
            id = "line-partial-update",
            alternateKeys =
            [
                new AlternateKey
                {
                    Key = "businesscentral.generaljournalline",
                    Value = Guid.NewGuid().ToString()
                }
            ],
            Journal = Ref<DHGeneralJournal>("journal-1"),
            Account = Ref<DHAccount>("account-1"),
            BalancingAccount = Ref<DHBank>("bank-1"),
            AccountType = "G/L Account",
            BalanceAccountType = "Bank Account",
            Amount = 25m
        };
        var cache = Cache<DHGeneralJournal>("journal-1", typeof(BCGeneralJournal).Name, Guid.NewGuid());
        cache[typeof(DHAccount).Name] = CacheObjects("account-1", typeof(BCAccount).Name, Guid.NewGuid());
        cache[typeof(DHBank).Name] = CacheObjects("bank-1", typeof(BCBank).Name, Guid.NewGuid());

        var result = await new MapGeneralJournalLineToBusinessCentralGeneralJournalLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Null(result.BalanceAccountType);
        Assert.Null(result.BalancingAccountId);
        Assert.Null(result.BalancingAccountNumber);
    }

    [Fact]
    public async Task GeneralJournalLineInboundUsesTypedReferences()
    {
        var accountId = Guid.NewGuid();
        var bankId = Guid.NewGuid();
        var result = await new MapBusinessCentralGeneralJournalLineToGeneralJournalLine().MapAsync(
            new BCGeneralLine { Id = Guid.NewGuid().ToString(), AccountType = "G_x002F_L_x0020_Account", AccountId = accountId, BalanceAccountType = "Bank_x0020_Account", BalancingAccountId = bankId },
            CancellationToken.None);

        AssertExternal<DHAccount, BCAccount>(result.Account, accountId);
        AssertExternal<DHBank, BCBank>(result.BalancingAccount, bankId);
        Assert.Equal("G/L Account", result.AccountType);
        Assert.Equal("Bank Account", result.BalanceAccountType);
    }

    [Fact]
    public async Task GeneralJournalLineRejectsAccountTypesOutsideTheStandardApiContract()
    {
        var source = new DHGeneralLine
        {
            id = "line-unsupported",
            Journal = Ref<DHGeneralJournal>("journal-1"),
            Account = Ref<DHAccount>("account-1"),
            AccountType = "Vendor"
        };
        var cache = Cache<DHGeneralJournal>("journal-1", typeof(BCGeneralJournal).Name, Guid.NewGuid());
        cache[typeof(DHAccount).Name] = CacheObjects("account-1", typeof(BCAccount).Name, Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapGeneralJournalLineToBusinessCentralGeneralJournalLine().MapAsync(source, CancellationToken.None, cache));

        Assert.Contains("Only G/L Account and Bank Account are supported", exception.Message);
    }

    [Fact]
    public async Task VendorPaymentDimensionMapsItsParentAndValue()
    {
        var paymentId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var source = new DHVendorDimension
        {
            id = "vendor-dimension-1",
            Payment = Ref<DHVendorPayment>("payment-1"),
            DimensionValue = Ref<DHValue>("value-1"),
            Code = "DEPARTMENT"
        };
        var cache = Cache<DHVendorPayment>("payment-1", typeof(BCVendorPayment).Name, paymentId);
        cache[typeof(DHValue).Name] = CacheObjects("value-1", "FinancialDimensionValue", valueId);

        var outbound = await new MapVendorPaymentDimensionToBusinessCentralVendorPaymentDimension()
            .MapAsync(source, CancellationToken.None, cache);
        var inbound = await new MapBusinessCentralVendorPaymentDimensionToVendorPaymentDimension()
            .MapAsync(new BCVendorDimension { Id = Guid.NewGuid().ToString(), ParentId = paymentId, ValueId = valueId, Code = source.Code }, CancellationToken.None);

        Assert.Equal(paymentId, outbound.ParentId);
        Assert.Equal(valueId, outbound.ValueId);
        AssertExternal<DHVendorPayment, BCVendorPayment>(inbound.Payment, paymentId);
        AssertExternal<DHValue, Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue>(inbound.DimensionValue, valueId);
    }

    [Fact]
    public async Task GeneralJournalDimensionMapsItsParentAndValue()
    {
        var lineId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var source = new DHGeneralDimension
        {
            id = "general-dimension-1",
            JournalLine = Ref<DHGeneralLine>("line-1"),
            DimensionValue = Ref<DHValue>("value-1"),
            Code = "DEPARTMENT"
        };
        var cache = Cache<DHGeneralLine>("line-1", typeof(BCGeneralLine).Name, lineId);
        cache[typeof(DHValue).Name] = CacheObjects("value-1", "FinancialDimensionValue", valueId);

        var outbound = await new MapGeneralJournalDimensionToBusinessCentralGeneralJournalDimension()
            .MapAsync(source, CancellationToken.None, cache);
        var inbound = await new MapBusinessCentralGeneralJournalDimensionToGeneralJournalDimension()
            .MapAsync(new BCGeneralDimension { Id = Guid.NewGuid().ToString(), ParentId = lineId, ValueId = valueId, Code = source.Code }, CancellationToken.None);

        Assert.Equal(lineId, outbound.ParentId);
        Assert.Equal(valueId, outbound.ValueId);
        AssertExternal<DHGeneralLine, BCGeneralLine>(inbound.JournalLine, lineId);
        AssertExternal<DHValue, Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue>(inbound.DimensionValue, valueId);
    }

    [Fact]
    public async Task LedgerMappingsAreInboundOnly()
    {
        var generalOutbound = new MapGeneralLedgerEntryToBusinessCentralGeneralLedgerEntry();
        var itemOutbound = new MapItemLedgerEntryToBusinessCentralItemLedgerEntry();
        var entry = await new MapBusinessCentralGeneralLedgerEntryToGeneralLedgerEntry().MapAsync(
            new BCLedger { Id = Guid.NewGuid().ToString(), EntryNumber = 10, DebitAmount = 12m }, CancellationToken.None);
        var itemEntry = await new MapBusinessCentralItemLedgerEntryToItemLedgerEntry().MapAsync(
            new BCItemLedger { Id = Guid.NewGuid().ToString(), EntryNumber = 11, ItemNumber = "ITEM", Quantity = 2m }, CancellationToken.None);

        Assert.Equal(10, entry.EntryNumber);
        Assert.Equal(12m, entry.DebitAmount);
        Assert.Equal("ITEM", itemEntry.ItemNumber);
        Assert.Equal(2m, itemEntry.Quantity);
        Assert.IsAssignableFrom<IDataHubTypeMapper<DHLedger, BCLedger>>(generalOutbound);
        Assert.IsAssignableFrom<IDataHubTypeMapper<DHItemLedger, BCItemLedger>>(itemOutbound);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            generalOutbound.MapAsync(new DHLedger(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            itemOutbound.MapAsync(new DHItemLedger(), CancellationToken.None));
    }

    private static string Url<T>() => Assert.Single(typeof(T).GetCustomAttributes(typeof(BusinessCentralUrlAttribute), true).Cast<BusinessCentralUrlAttribute>()).Url;
    private static void AssertParent<T>(string url, string property)
    {
        var parent = Assert.Single(typeof(T).GetCustomAttributes(typeof(BusinessCentralParentUrlAttribute), true).Cast<BusinessCentralParentUrlAttribute>());
        Assert.Equal(url, parent.ParentUrl);
        Assert.Equal(property, parent.ParentIdPropertyName);
    }
    private static EntityReference Ref<T>(string id) => new() { EntityType = typeof(T).Name, EntityId = id };
    private static void AssertExternal<TDataHub, TBusinessCentral>(EntityReference? reference, Guid id)
    {
        var external = Assert.IsType<ExternalEntityReference>(reference);
        Assert.Equal(typeof(TDataHub).Name, external.EntityType);
        Assert.Equal(typeof(TBusinessCentral).Name, external.SourceEntityType);
        Assert.Equal(id.ToString(), external.EntityId);
    }
    private static Dictionary<string, object> Cache<T>(string id, string type, Guid externalId) where T : DataHubEntity =>
        new() { [typeof(T).Name] = CacheObjects(id, type, externalId) };
    private static List<JObject> CacheObjects(string id, string type, Guid externalId) =>
        [new JObject { [nameof(DataHubEntity.id)] = id, [nameof(DataHubEntity.alternateKeys)] = new JArray(new JObject { [nameof(AlternateKey.Key)] = $"businesscentral.{type}".ToLowerInvariant(), [nameof(AlternateKey.Value)] = externalId.ToString() }) }];
}
