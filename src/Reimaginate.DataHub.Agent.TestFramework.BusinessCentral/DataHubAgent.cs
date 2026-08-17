using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Test.Framework;
using Reimaginate.Test.Framework.Helpers;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral;

public sealed class DataHubAgent : TestAgentBase<DataHubAgent>
{
    private const string DefaultDataSource = "BusinessCentralIntegrationSeed";

    public DataHubAgent()
    {
    }

    public DataHubAgent(IServiceProvider serviceProvider)
    {
        AgentServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        HostServices = serviceProvider;
        ActivitySource = DiagnosticConfig.DataHubAgent.ActivitySource;
    }

    public DataHubAgent(Func<ServiceCollection> serviceCollectionBuilder)
        : base(serviceCollectionBuilder, DiagnosticConfig.DataHubAgent.ActivitySource)
    {
    }

    private IDataHubClient DataHubClient => AgentServices.GetRequiredService<IDataHubClient>();

    public DataHubAgent CreateDataHubEntity<TDataHubEntity>(
        TDataHubEntity entity,
        string? stashTo = null,
        string? sourceEntityType = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingEntity = await CreateEntity(entity, sourceEntityType);
            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = resultingEntity;

            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public DataHubAgent CreateDataHubEntity<TDataHubEntity>(
        Func<object, Dictionary<string, object?>, TDataHubEntity> entityFunc,
        string? stashTo = null,
        string? sourceEntityType = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingEntity = await CreateEntity(entityFunc(currentObject, stash), sourceEntityType);
            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public DataHubAgent GetDataHubEntityFromStash<TDataHubEntity>(string fromStash, string? stashTo = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var source = stash[fromStash].ToObject<TDataHubEntity>()
                ?? throw new InvalidOperationException($"Data Hub entity '{fromStash}' was not found in the scenario stash.");
            var resultingEntity = await GetEntity<TDataHubEntity>(source.id);
            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = resultingEntity;

            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public DataHubAgent PatchDataHubEntity<TDataHubEntity>(
        string fromStash,
        Func<TDataHubEntity, List<Patch>> patchFunc,
        string? stashTo = null)
        where TDataHubEntity : DataHubEntity
        => PatchDataHubEntity<TDataHubEntity>(fromStash, (entity, _) => patchFunc(entity), stashTo);

    public DataHubAgent PatchDataHubEntity<TDataHubEntity>(
        string fromStash,
        Func<TDataHubEntity, Dictionary<string, object?>, List<Patch>> patchFunc,
        string? stashTo = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var entity = stash[fromStash].ToObject<TDataHubEntity>()
                ?? throw new InvalidOperationException($"Data Hub entity '{fromStash}' was not found in the scenario stash.");
            var operations = patchFunc(entity, stash);
            var response = await DataHubClient.PostRequestAsync<PatchEntitiesRequest, PatchEntitiesResponse>(
                new PatchEntitiesRequest
                {
                    DispatchNotifications = false,
                    Requests =
                    [
                        new PatchEntityRequest
                        {
                            DataSource = DataSources.DataHub,
                            EntityType = entity.entityType,
                            EntityId = entity.id,
                            Operations = operations
                        }
                    ]
                },
                CancellationToken.None);

            if (!response.Success)
            {
                var failures = response.Results
                    .Where(result => !result.Success)
                    .Select(result => result.FailureReason)
                    .Where(reason => !string.IsNullOrWhiteSpace(reason));
                var message = string.Join(Environment.NewLine, failures);
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = $"Response: {JsonConvert.SerializeObject(response)}. Operations: {JsonConvert.SerializeObject(operations)}";
                }

                throw new InvalidOperationException($"Data Hub patch failed: {message}");
            }

            var resultingEntity = await GetEntity<TDataHubEntity>(entity.id);
            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = resultingEntity;

            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public DataHubAgent DeleteDataHubEntityFromStash<TDataHubEntity>(string fromStash)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            if (stash.TryGetValue(fromStash, out var value))
            {
                var entityId = value switch
                {
                    string id => id,
                    DataHubEntity entity => entity.id,
                    _ => value?.ToObject<TDataHubEntity>()?.id
                };

                if (!string.IsNullOrWhiteSpace(entityId))
                {
                    try
                    {
                        await DataHubClient.PostRequestAsync<DeleteDataHubEntitiesRequest, DeleteDataHubEntitiesResponse>(
                            new DeleteDataHubEntitiesRequest
                            {
                                EntityType = typeof(TDataHubEntity).Name,
                                EntityIds = [entityId],
                                IncludeTrackingEntries = true
                            },
                            CancellationToken.None);
                    }
                    catch
                    {
                        // Finally cleanup is best-effort and restricted to a test-created entity id.
                    }
                }
            }

            return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    private async Task<TDataHubEntity> GetEntity<TDataHubEntity>(string entityId)
        where TDataHubEntity : DataHubEntity
    {
        var response = await DataHubClient.PostRequestAsync<GetDataHubEntityRequest, GetDataHubEntityResponse>(
            new GetDataHubEntityRequest
            {
                EntityType = typeof(TDataHubEntity).Name,
                EntityId = entityId
            },
            CancellationToken.None);

        return response.Entity.ToObject<TDataHubEntity>()
            ?? throw new InvalidOperationException($"Data Hub entity '{typeof(TDataHubEntity).Name}/{entityId}' was not found.");
    }

    private async Task<TDataHubEntity> CreateEntity<TDataHubEntity>(
        TDataHubEntity entity,
        string? sourceEntityType)
        where TDataHubEntity : DataHubEntity
    {
        var response = await DataHubClient.PostRequestAsync<MergeEntitiesRequest, MergeEntitiesResponse>(
            new MergeEntitiesRequest
            {
                DataSource = DefaultDataSource,
                Requests =
                [
                    new MergeEntityRequest
                    {
                        DataSource = DefaultDataSource,
                        DataHubEntityType = entity.entityType,
                        SourceEntityType = sourceEntityType ?? typeof(TDataHubEntity).Name,
                        SourceEntityId = entity.id,
                        Data = JObject.FromObject(entity)
                    }
                ]
            },
            CancellationToken.None);

        var result = response.Results.SingleOrDefault()
            ?? throw new InvalidOperationException("Data Hub seed merge returned no result.");
        if (!MergeOutcomes.IsSuccess(result.MergeOutcome))
        {
            throw new InvalidOperationException($"Data Hub seed merge failed: {result.FailureReason}");
        }

        return await GetEntity<TDataHubEntity>(result.DataHubEntityId);
    }
}
