using Reimaginate.Mapper;
using BusinessCentralCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentJournal;
using BusinessCentralGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using DataHubCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentJournal;
using DataHubGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralCustomerPaymentJournalToCustomerPaymentJournal :
    ITypeMapper<BusinessCentralCustomerPaymentJournal, DataHubCustomerPaymentJournal>
{
    public Task<DataHubCustomerPaymentJournal> MapAsync(
        BusinessCentralCustomerPaymentJournal from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubCustomerPaymentJournal
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code,
            DisplayName = from.DisplayName,
            BalancingAccount = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubGeneralLedgerAccount, BusinessCentralGeneralLedgerAccount>(from.BalancingAccountId),
            BalancingAccountNumber = from.BalancingAccountNumber
        });
}
