using System.Security.Cryptography;
using System.Text;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using BCAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using BCBank = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.BankAccount;
using BCJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournal;
using BCLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalLine;
using DHAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;
using DHBank = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.BankAccount;
using DHJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournal;
using DHLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournalLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapGeneralJournalToBusinessCentralGeneralJournal : ITypeMapper<DHJournal, BCJournal>, IDataHubTypeMapper<DHJournal, BCJournal>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DHJournal.BalancingAccount)];
    public Task<BCJournal> MapAsync(DHJournal from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        Guid? balancingId = null;
        if (from.BalancingAccount is not null)
        {
            balancingId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHAccount>(from.BalancingAccount, typeof(BCAccount).Name, cache);
            if (!balancingId.HasValue) throw new InvalidOperationException($"General journal '{from.id}' requires a tracked G/L balancing account.");
        }
        var code = from.Code;
        var tracked = from.alternateKeys?.Any(key => key.Key.Equals("businesscentral.generaljournal", StringComparison.OrdinalIgnoreCase)) == true;
        if (string.IsNullOrWhiteSpace(code) && !tracked)
            code = "DG" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(from.id)))[..8];
        return Task.FromResult(new BCJournal { Code = code, DisplayName = from.DisplayName, BalancingAccountId = balancingId });
    }
}

public sealed class MapBusinessCentralGeneralJournalToGeneralJournal : ITypeMapper<BCJournal, DHJournal>
{
    public Task<DHJournal> MapAsync(BCJournal from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHJournal
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code, DisplayName = from.DisplayName, TemplateDisplayName = from.TemplateDisplayName,
            BalancingAccount = BusinessCentralMappingHelpers.ToDataHubReference<DHAccount, BCAccount>(from.BalancingAccountId),
            BalancingAccountNumber = from.BalancingAccountNumber
        });
}

public sealed class MapGeneralJournalLineToBusinessCentralGeneralJournalLine : ITypeMapper<DHLine, BCLine>, IDataHubTypeMapper<DHLine, BCLine>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DHLine.Journal), nameof(DHLine.Account), nameof(DHLine.BalancingAccount)];
    public Task<BCLine> MapAsync(DHLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        if (from.Journal is null || from.Account is null)
            throw new InvalidOperationException("A general journal line must reference a journal and an account.");
        var journalId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHJournal>(from.Journal, typeof(BCJournal).Name, cache);
        var account = ResolveAccount(from.Account, from.AccountType, cache);
        var balancing = from.BalancingAccount is null
            ? (Id: (Guid?)null, Type: ValidateOptionalAccountType(from.BalanceAccountType))
            : ResolveAccount(from.BalancingAccount, from.BalanceAccountType, cache);
        if (!journalId.HasValue || !account.Id.HasValue)
            throw new InvalidOperationException($"General journal line '{from.id}' requires tracked Business Central journal and account ids.");

        var balancingAccountIsBank = string.Equals(
            balancing.Type,
            "Bank Account",
            StringComparison.OrdinalIgnoreCase);
        var tracked = from.alternateKeys?.Any(key =>
            key.Key.Equals("businesscentral.generaljournalline", StringComparison.OrdinalIgnoreCase)) == true;
        if (balancingAccountIsBank && string.IsNullOrWhiteSpace(from.BalancingAccountNumber) && !tracked)
        {
            throw new InvalidOperationException(
                $"General journal line '{from.id}' requires BalancingAccountNumber when its balancing account is a Bank Account. " +
                "The standard Business Central journalLines API resolves bank balancing accounts by number during create and update.");
        }

        var result = new BCLine
        {
            JournalId = journalId, AccountType = account.Type, AccountId = account.Id,
            PostingDate = from.PostingDate, DocumentNumber = from.DocumentNumber,
            ExternalDocumentNumber = from.ExternalDocumentNumber, Amount = from.Amount,
            Description = from.Description, Comment = from.Comment, TaxCode = from.TaxCode
        };
        if (balancingAccountIsBank)
        {
            // Data Hub patch responses can omit unchanged values. For a tracked line, leaving the
            // balancing fields untouched is safer than clearing the existing bank account.
            if (!string.IsNullOrWhiteSpace(from.BalancingAccountNumber))
            {
                result.BalanceAccountType = balancing.Type;
                result.BalancingAccountNumber = from.BalancingAccountNumber;
            }
        }
        else
        {
            result.BalanceAccountType = balancing.Type;
            result.BalancingAccountId = balancing.Id;
        }

        return Task.FromResult(result);
    }

    private static (Guid? Id, string? Type) ResolveAccount(EntityReference reference, string? requestedType, Dictionary<string, object>? cache)
    {
        var referenceIsBank = reference.EntityType.Equals(typeof(DHBank).Name, StringComparison.OrdinalIgnoreCase);
        var referenceIsGl = reference.EntityType.Equals(typeof(DHAccount).Name, StringComparison.OrdinalIgnoreCase);
        if (!referenceIsBank && !referenceIsGl)
            throw new InvalidOperationException(
                $"General journal lines support only {typeof(DHAccount).Name} and {typeof(DHBank).Name} account references through the standard API.");

        var normalizedType = string.IsNullOrWhiteSpace(requestedType)
            ? referenceIsBank ? "Bank Account" : "G/L Account"
            : ValidateAccountType(requestedType);
        if (referenceIsBank != string.Equals(normalizedType, "Bank Account", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"General journal account reference type '{reference.EntityType}' does not match requested Business Central type '{requestedType}'.");

        if (referenceIsBank)
            return (BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHBank>(reference, typeof(BCBank).Name, cache), "Bank Account");
        return (BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHAccount>(reference, typeof(BCAccount).Name, cache), "G/L Account");
    }

    private static string ValidateAccountType(string requestedType)
    {
        if (string.Equals(requestedType, "G/L Account", StringComparison.OrdinalIgnoreCase)) return "G/L Account";
        if (string.Equals(requestedType, "Bank Account", StringComparison.OrdinalIgnoreCase)) return "Bank Account";
        throw new InvalidOperationException(
            $"General journal account type '{requestedType}' is not exposed by the standard v2 journalLines API. Only G/L Account and Bank Account are supported.");
    }

    private static string? ValidateOptionalAccountType(string? requestedType) =>
        string.IsNullOrWhiteSpace(requestedType) ? requestedType : ValidateAccountType(requestedType);
}

public sealed class MapBusinessCentralGeneralJournalLineToGeneralJournalLine : ITypeMapper<BCLine, DHLine>
{
    public Task<DHLine> MapAsync(BCLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        var accountType = NormalizeBusinessCentralAccountType(from.AccountType);
        var balancingAccountType = NormalizeBusinessCentralAccountType(from.BalanceAccountType);
        return Task.FromResult(new DHLine
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Journal = BusinessCentralMappingHelpers.ToDataHubReference<DHJournal, BCJournal>(from.JournalId),
            Account = AccountReference(accountType, from.AccountId),
            BalancingAccount = AccountReference(balancingAccountType, from.BalancingAccountId),
            LineNumber = from.LineNumber, AccountType = accountType, AccountNumber = from.AccountNumber,
            PostingDate = from.PostingDate, DocumentNumber = from.DocumentNumber,
            ExternalDocumentNumber = from.ExternalDocumentNumber, Amount = from.Amount,
            Description = from.Description, Comment = from.Comment, TaxCode = from.TaxCode,
            BalanceAccountType = balancingAccountType, BalancingAccountNumber = from.BalancingAccountNumber
        });
    }

    private static string? NormalizeBusinessCentralAccountType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return type;
        if (string.Equals(type, "G/L Account", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "G_x002F_L_x0020_Account", StringComparison.OrdinalIgnoreCase))
        {
            return "G/L Account";
        }

        if (string.Equals(type, "Bank Account", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "Bank_x0020_Account", StringComparison.OrdinalIgnoreCase))
        {
            return "Bank Account";
        }

        throw new InvalidOperationException(
            $"Business Central returned unsupported general journal account type '{type}'.");
    }

    private static EntityReference? AccountReference(string? type, Guid? id)
    {
        if (!id.HasValue || id == Guid.Empty) return null;
        if (string.Equals(type, "Bank Account", StringComparison.OrdinalIgnoreCase))
            return BusinessCentralMappingHelpers.ToDataHubReference<DHBank, BCBank>(id);
        if (string.Equals(type, "G/L Account", StringComparison.OrdinalIgnoreCase))
            return BusinessCentralMappingHelpers.ToDataHubReference<DHAccount, BCAccount>(id);
        throw new InvalidOperationException(
            $"Business Central returned unsupported general journal account type '{type ?? "<missing>"}'.");
    }
}
