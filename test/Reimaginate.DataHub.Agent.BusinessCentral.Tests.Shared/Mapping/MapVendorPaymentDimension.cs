using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BCLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPaymentDimensionSetLine;
using BCPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPayment;
using BCValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue;
using DHLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPaymentDimension;
using DHPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPayment;
using DHValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapVendorPaymentDimensionToBusinessCentralVendorPaymentDimension : ITypeMapper<DHLine, BCLine>, IDataHubTypeMapper<DHLine, BCLine>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DHLine.Payment), nameof(DHLine.DimensionValue)];
    public Task<BCLine> MapAsync(DHLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        if (from.Payment is null || from.DimensionValue is null || string.IsNullOrWhiteSpace(from.Code))
            throw new InvalidOperationException("A vendor payment dimension requires a payment, dimension code, and dimension value.");
        var parentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHPayment>(from.Payment, typeof(BCPayment).Name, cache);
        var valueId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHValue>(from.DimensionValue, typeof(BCValue).Name, cache);
        if (!parentId.HasValue || !valueId.HasValue)
            throw new InvalidOperationException($"Vendor payment dimension '{from.id}' requires tracked payment and dimension-value ids.");
        return Task.FromResult(new BCLine { ParentId = parentId, Code = from.Code, ValueId = valueId });
    }
}

public sealed class MapBusinessCentralVendorPaymentDimensionToVendorPaymentDimension : ITypeMapper<BCLine, DHLine>
{
    public Task<DHLine> MapAsync(BCLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHLine
        {
            id = from.Id!, createdOn = DateTimeOffset.UnixEpoch, lastUpdated = DateTimeOffset.UnixEpoch,
            Payment = BusinessCentralMappingHelpers.ToDataHubReference<DHPayment, BCPayment>(from.ParentId),
            DimensionValue = BusinessCentralMappingHelpers.ToDataHubReference<DHValue, BCValue>(from.ValueId),
            Code = from.Code, DisplayName = from.DisplayName, ValueCode = from.ValueCode, ValueDisplayName = from.ValueDisplayName
        });
}
