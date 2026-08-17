using Reimaginate.Mapper;
using BusinessCentralBankAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.BankAccount;
using BusinessCentralCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Currency;
using BusinessCentralFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimension;
using BusinessCentralFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue;
using BusinessCentralGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using DataHubBankAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.BankAccount;
using DataHubCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Currency;
using DataHubFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimension;
using DataHubFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;
using DataHubGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralGeneralLedgerAccountToGeneralLedgerAccount :
    ITypeMapper<BusinessCentralGeneralLedgerAccount, DataHubGeneralLedgerAccount>
{
    public Task<DataHubGeneralLedgerAccount> MapAsync(BusinessCentralGeneralLedgerAccount from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubGeneralLedgerAccount
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Number = from.Number,
            DisplayName = from.DisplayName,
            Category = from.Category,
            SubCategory = from.SubCategory,
            Blocked = from.Blocked,
            AccountType = from.AccountType,
            DirectPosting = from.DirectPosting,
            NetChange = from.NetChange
        });
}

public sealed class MapBusinessCentralBankAccountToBankAccount :
    ITypeMapper<BusinessCentralBankAccount, DataHubBankAccount>
{
    public Task<DataHubBankAccount> MapAsync(BusinessCentralBankAccount from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubBankAccount
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Number = from.Number,
            DisplayName = from.DisplayName,
            BankAccountNumber = from.BankAccountNumberValue,
            Blocked = from.Blocked,
            CurrencyCode = from.CurrencyCode,
            Currency = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubCurrency, BusinessCentralCurrency>(from.CurrencyId),
            Iban = from.Iban,
            IntercompanyEnabled = from.IntercompanyEnabled
        });
}

public sealed class MapBusinessCentralFinancialDimensionToFinancialDimension :
    ITypeMapper<BusinessCentralFinancialDimension, DataHubFinancialDimension>
{
    public Task<DataHubFinancialDimension> MapAsync(BusinessCentralFinancialDimension from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubFinancialDimension
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code,
            DisplayName = from.DisplayName,
            ConsolidationCode = from.ConsolidationCode
        });
}

public sealed class MapBusinessCentralFinancialDimensionValueToFinancialDimensionValue :
    ITypeMapper<BusinessCentralFinancialDimensionValue, DataHubFinancialDimensionValue>
{
    public Task<DataHubFinancialDimensionValue> MapAsync(BusinessCentralFinancialDimensionValue from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubFinancialDimensionValue
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Dimension = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubFinancialDimension, BusinessCentralFinancialDimension>(from.DimensionId),
            Code = from.Code,
            DisplayName = from.DisplayName,
            ConsolidationCode = from.ConsolidationCode
        });
}
