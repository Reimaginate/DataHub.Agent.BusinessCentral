using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPayment;
using BusinessCentralCustomerPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentDimensionSetLine;
using BusinessCentralFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue;
using DataHubCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPayment;
using DataHubCustomerPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentDimension;
using DataHubFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapCustomerPaymentDimensionToBusinessCentralCustomerPaymentDimension :
    ITypeMapper<DataHubCustomerPaymentDimension, BusinessCentralCustomerPaymentDimension>,
    IDataHubTypeMapper<DataHubCustomerPaymentDimension, BusinessCentralCustomerPaymentDimension>
{
    public List<string> MappedEntityReferences { get; } =
    [nameof(DataHubCustomerPaymentDimension.Payment), nameof(DataHubCustomerPaymentDimension.DimensionValue)];

    public Task<BusinessCentralCustomerPaymentDimension> MapAsync(
        DataHubCustomerPaymentDimension from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (from.Payment is null || from.DimensionValue is null || string.IsNullOrWhiteSpace(from.Code))
        {
            throw new InvalidOperationException(
                "A customer payment dimension requires a payment, dimension code, and dimension value.");
        }

        var paymentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubCustomerPayment>(
            from.Payment,
            typeof(BusinessCentralCustomerPayment).Name,
            cache);
        var valueId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DataHubFinancialDimensionValue>(
            from.DimensionValue,
            typeof(BusinessCentralFinancialDimensionValue).Name,
            cache);
        if (!paymentId.HasValue || !valueId.HasValue)
        {
            throw new InvalidOperationException(
                $"Customer payment dimension '{from.id}' requires tracked Business Central payment and dimension-value ids.");
        }

        return Task.FromResult(new BusinessCentralCustomerPaymentDimension
        {
            ParentId = paymentId,
            Code = from.Code,
            ValueId = valueId
        });
    }
}

public sealed class MapBusinessCentralCustomerPaymentDimensionToCustomerPaymentDimension :
    ITypeMapper<BusinessCentralCustomerPaymentDimension, DataHubCustomerPaymentDimension>
{
    public Task<DataHubCustomerPaymentDimension> MapAsync(
        BusinessCentralCustomerPaymentDimension from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DataHubCustomerPaymentDimension
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            Payment = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubCustomerPayment, BusinessCentralCustomerPayment>(from.ParentId),
            DimensionValue = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubFinancialDimensionValue, BusinessCentralFinancialDimensionValue>(from.ValueId),
            Code = from.Code,
            DisplayName = from.DisplayName,
            ValueCode = from.ValueCode,
            ValueDisplayName = from.ValueDisplayName
        });
}
