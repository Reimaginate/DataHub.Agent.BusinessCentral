using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeUpdatedBusinessCentralEntities
{
    public class MergeUpdatedBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity> : IRequest<NullResponse> where TBusinessCentralEntity : BusinessCentralDocument, IBusinessCentralIncrementalEntity where TDataHubEntity : DataHubEntity
    {
        public string CorrelationId { get; set; }
        public DateTime? FromDateTime { get; set; }
        public int BatchSize { get; set; } = 500;
    }
}
