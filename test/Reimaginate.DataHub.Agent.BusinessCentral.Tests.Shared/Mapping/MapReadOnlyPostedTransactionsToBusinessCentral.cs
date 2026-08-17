using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceipt;
using BusinessCentralPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceiptLine;
using BusinessCentralSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipment;
using BusinessCentralSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipmentLine;
using DataHubPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceipt;
using DataHubPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceiptLine;
using DataHubSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipment;
using DataHubSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipmentLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSalesShipmentToBusinessCentralSalesShipment :
    ITypeMapper<DataHubSalesShipment, BusinessCentralSalesShipment>,
    IDataHubTypeMapper<DataHubSalesShipment, BusinessCentralSalesShipment>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralSalesShipment> MapAsync(DataHubSalesShipment from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw ReadOnly(nameof(BusinessCentralSalesShipment));

    private static NotSupportedException ReadOnly(string name) =>
        new($"Business Central {name} is read-only in the standard API and cannot be synced outbound.");
}

public sealed class MapSalesShipmentLineToBusinessCentralSalesShipmentLine :
    ITypeMapper<DataHubSalesShipmentLine, BusinessCentralSalesShipmentLine>,
    IDataHubTypeMapper<DataHubSalesShipmentLine, BusinessCentralSalesShipmentLine>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralSalesShipmentLine> MapAsync(DataHubSalesShipmentLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new NotSupportedException(
            "Business Central SalesShipmentLine is read-only in the standard API and cannot be synced outbound.");
}

public sealed class MapPurchaseReceiptToBusinessCentralPurchaseReceipt :
    ITypeMapper<DataHubPurchaseReceipt, BusinessCentralPurchaseReceipt>,
    IDataHubTypeMapper<DataHubPurchaseReceipt, BusinessCentralPurchaseReceipt>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralPurchaseReceipt> MapAsync(DataHubPurchaseReceipt from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new NotSupportedException(
            "Business Central PurchaseReceipt is read-only in the standard API and cannot be synced outbound.");
}

public sealed class MapPurchaseReceiptLineToBusinessCentralPurchaseReceiptLine :
    ITypeMapper<DataHubPurchaseReceiptLine, BusinessCentralPurchaseReceiptLine>,
    IDataHubTypeMapper<DataHubPurchaseReceiptLine, BusinessCentralPurchaseReceiptLine>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BusinessCentralPurchaseReceiptLine> MapAsync(DataHubPurchaseReceiptLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new NotSupportedException(
            "Business Central PurchaseReceiptLine is read-only in the standard API and cannot be synced outbound.");
}
