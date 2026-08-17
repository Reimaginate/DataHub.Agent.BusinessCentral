using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming


namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Queries.GetSpecificBusinessCentralEntities;

public class GetSpecificBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity> : IHandler<GetSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity>, List<TBusinessCentralEntity>> where TBusinessCentralEntity : BusinessCentralDocument
{
    private readonly IBusinessCentralODataService _businessCentralService;

    public GetSpecificBusinessCentralEntitiesRequestHandler(IBusinessCentralODataService businessCentralService)
    {
        _businessCentralService = businessCentralService;
    }

    private static string GetQueryString(string idValue)
    {
        return Guid.TryParse(idValue, out _) ? $"id eq {idValue}" : $"no eq '{idValue.Replace("'", "''")}'";
    }

    public async Task<List<TBusinessCentralEntity>> HandleAsync(GetSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity> request, CancellationToken cancellationToken)
    {
        // The standard Business Central API rejects compound OR predicates for GUID ids in
        // some entity sets (notably salesOrderLines) with Application_InvalidGUID. Resolve
        // each requested key independently and aggregate the records so one parser quirk
        // cannot fail an otherwise valid multi-record synchronization request.
        var results = new List<TBusinessCentralEntity>();
        foreach (var entityId in request.EntityIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var response = await _businessCentralService.GetEntitiesAsync<TBusinessCentralEntity>(
                GetQueryString(entityId),
                cancellationToken: cancellationToken);
            if (response.IsT1)
            {
                var responseContent = await response.AsT1.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception(response.AsT1.ReasonPhrase,
                    !string.IsNullOrEmpty(responseContent) ? new Exception(responseContent) : null);
            }
            if (response.IsT2) throw response.AsT2;

            results.AddRange(response.AsT0.Value);
        }

        return results;
    }
}
