using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BCAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using BCGeneral = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerEntry;
using BCItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.ItemLedgerEntry;
using DHAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;
using DHGeneral = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerEntry;
using DHItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.ItemLedgerEntry;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralGeneralLedgerEntryToGeneralLedgerEntry : ITypeMapper<BCGeneral, DHGeneral>
{
    public Task<DHGeneral> MapAsync(BCGeneral from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHGeneral
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Account = BusinessCentralMappingHelpers.ToDataHubReference<DHAccount, BCAccount>(from.AccountId),
            EntryNumber = from.EntryNumber, PostingDate = from.PostingDate, DocumentNumber = from.DocumentNumber,
            DocumentType = from.DocumentType, AccountNumber = from.AccountNumber,
            Description = from.Description, DebitAmount = from.DebitAmount, CreditAmount = from.CreditAmount,
            AdditionalCurrencyDebitAmount = from.AdditionalCurrencyDebitAmount,
            AdditionalCurrencyCreditAmount = from.AdditionalCurrencyCreditAmount
        });
}

public sealed class MapBusinessCentralItemLedgerEntryToItemLedgerEntry : ITypeMapper<BCItem, DHItem>
{
    public Task<DHItem> MapAsync(BCItem from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHItem
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            EntryNumber = from.EntryNumber, ItemNumber = from.ItemNumber, PostingDate = from.PostingDate,
            EntryType = from.EntryType, SourceNumber = from.SourceNumber, SourceType = from.SourceType,
            DocumentNumber = from.DocumentNumber, DocumentType = from.DocumentType, Description = from.Description,
            Quantity = from.Quantity, SalesAmountActual = from.SalesAmountActual, CostAmountActual = from.CostAmountActual
        });
}

public sealed class MapGeneralLedgerEntryToBusinessCentralGeneralLedgerEntry :
    ITypeMapper<DHGeneral, BCGeneral>,
    IDataHubTypeMapper<DHGeneral, BCGeneral>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BCGeneral> MapAsync(DHGeneral from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new InvalidOperationException("Business Central general ledger entries are read-only transaction results.");
}

public sealed class MapItemLedgerEntryToBusinessCentralItemLedgerEntry :
    ITypeMapper<DHItem, BCItem>,
    IDataHubTypeMapper<DHItem, BCItem>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BCItem> MapAsync(DHItem from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new InvalidOperationException("Business Central item ledger entries are read-only transaction results.");
}
