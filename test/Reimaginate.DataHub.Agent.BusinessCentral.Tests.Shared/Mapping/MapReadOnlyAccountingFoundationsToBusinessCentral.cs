using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralBankAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.BankAccount;
using BusinessCentralFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimension;
using BusinessCentralFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue;
using BusinessCentralGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using DataHubBankAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.BankAccount;
using DataHubFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimension;
using DataHubFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;
using DataHubGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

internal static class ReadOnlyAccountingFoundation
{
    public static NotSupportedException Error<T>() =>
        new($"Business Central {typeof(T).Name} is treated as accounting configuration and is inbound-only.");
}

public sealed class MapGeneralLedgerAccountToBusinessCentralGeneralLedgerAccount :
    ITypeMapper<DataHubGeneralLedgerAccount, BusinessCentralGeneralLedgerAccount>,
    IDataHubTypeMapper<DataHubGeneralLedgerAccount, BusinessCentralGeneralLedgerAccount>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralGeneralLedgerAccount> MapAsync(DataHubGeneralLedgerAccount from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw ReadOnlyAccountingFoundation.Error<BusinessCentralGeneralLedgerAccount>();
}

public sealed class MapBankAccountToBusinessCentralBankAccount :
    ITypeMapper<DataHubBankAccount, BusinessCentralBankAccount>,
    IDataHubTypeMapper<DataHubBankAccount, BusinessCentralBankAccount>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralBankAccount> MapAsync(DataHubBankAccount from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw ReadOnlyAccountingFoundation.Error<BusinessCentralBankAccount>();
}

public sealed class MapFinancialDimensionToBusinessCentralFinancialDimension :
    ITypeMapper<DataHubFinancialDimension, BusinessCentralFinancialDimension>,
    IDataHubTypeMapper<DataHubFinancialDimension, BusinessCentralFinancialDimension>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralFinancialDimension> MapAsync(DataHubFinancialDimension from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw ReadOnlyAccountingFoundation.Error<BusinessCentralFinancialDimension>();
}

public sealed class MapFinancialDimensionValueToBusinessCentralFinancialDimensionValue :
    ITypeMapper<DataHubFinancialDimensionValue, BusinessCentralFinancialDimensionValue>,
    IDataHubTypeMapper<DataHubFinancialDimensionValue, BusinessCentralFinancialDimensionValue>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralFinancialDimensionValue> MapAsync(DataHubFinancialDimensionValue from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw ReadOnlyAccountingFoundation.Error<BusinessCentralFinancialDimensionValue>();
}
