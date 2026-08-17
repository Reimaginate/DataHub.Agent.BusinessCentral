using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;

public interface IDataHubTypeMapper<TDataHubEntity, TBusinessCentralEntity>
    where TDataHubEntity : DataHubEntity
    where TBusinessCentralEntity : BusinessCentralDocument
{
    List<string> MappedEntityReferences { get; }
}
