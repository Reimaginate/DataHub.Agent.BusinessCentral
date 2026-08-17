using System.Security.Cryptography;
using System.Text;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentJournal;
using BusinessCentralGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using DataHubCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentJournal;
using DataHubGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapCustomerPaymentJournalToBusinessCentralCustomerPaymentJournal :
    ITypeMapper<DataHubCustomerPaymentJournal, BusinessCentralCustomerPaymentJournal>,
    IDataHubTypeMapper<DataHubCustomerPaymentJournal, BusinessCentralCustomerPaymentJournal>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DataHubCustomerPaymentJournal.BalancingAccount)];

    public Task<BusinessCentralCustomerPaymentJournal> MapAsync(
        DataHubCustomerPaymentJournal from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var tracked = from.alternateKeys?.Any(key =>
            key.Key.Equals("businesscentral.customerpaymentjournal", StringComparison.OrdinalIgnoreCase)) == true;
        var code = from.Code;
        if (string.IsNullOrWhiteSpace(code) && !tracked)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(from.id)));
            code = $"DH{hash[..8]}";
        }

        Guid? balancingAccountId = null;
        if (from.BalancingAccount is not null)
        {
            balancingAccountId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubGeneralLedgerAccount>(
                from.BalancingAccount,
                typeof(BusinessCentralGeneralLedgerAccount).Name,
                cache);
            if (!balancingAccountId.HasValue)
            {
                throw new InvalidOperationException(
                    $"The balancing account referenced by customer payment journal '{from.id}' has no Business Central G/L account id.");
            }
        }

        var result = new BusinessCentralCustomerPaymentJournal
        {
            Code = code,
            DisplayName = string.IsNullOrWhiteSpace(from.DisplayName) ? $"Data Hub {code}" : from.DisplayName
        };
        if (balancingAccountId.HasValue)
        {
            result.BalancingAccountId = balancingAccountId;
        }

        return Task.FromResult(result);
    }
}
