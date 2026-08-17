using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeUpdatedBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralSyncMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.TestFramework;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using Reimaginate.Test.Framework;
using Reimaginate.Test.Framework.Helpers;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral;

public sealed class BusinessCentralAgent : TestAgentBase<BusinessCentralAgent>
{
    public BusinessCentralAgent()
        : this((string?)null)
    {
    }

    public BusinessCentralAgent(string? instanceId)
    {
        InstanceId = string.IsNullOrWhiteSpace(instanceId)
            ? new AgentTestInstance("BusinessCentralAgent").Id
            : instanceId;
    }

    public BusinessCentralAgent(IServiceProvider serviceProvider)
        : this(serviceProvider?.GetService<AgentTestInstance>()?.Id)
    {
        AgentServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        HostServices = serviceProvider;
        ActivitySource = DiagnosticConfig.BusinessCentralAgent.ActivitySource;
    }

    public BusinessCentralAgent(Func<ServiceCollection> serviceCollectionBuilder)
        : base(serviceCollectionBuilder, DiagnosticConfig.BusinessCentralAgent.ActivitySource)
    {
        InstanceId = new AgentTestInstance("BusinessCentralAgent").Id;
    }

    public string InstanceId { get; }

    public MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity> MergeSpecific<TBusinessCentralEntity, TDataHubEntity>(
        IEnumerable<string> entityIds,
        bool forceUpdate = false)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        return new MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>(entityIds.ToList(), InstanceId)
        {
            ForceUpdate = forceUpdate
        };
    }

    public SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity> SyncSpecific<TDataHubEntity, TBusinessCentralEntity>(
        IEnumerable<string> entityIds)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        return new SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>
        {
            EntityIds = entityIds.ToList(),
            CorrelationId = InstanceId
        };
    }

    public BusinessCentralAgent CreateRecord<TBusinessCentralEntity>(
        TBusinessCentralEntity record,
        string? stashTo = null)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        return CreateRecord<TBusinessCentralEntity>((_, _) => record, stashTo);
    }

    public BusinessCentralAgent CreateRecord<TBusinessCentralEntity>(
        Func<object, Dictionary<string, object?>, TBusinessCentralEntity> recordFactory,
        string? stashTo = null)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var result = await AgentServices.GetRequiredService<IBusinessCentralODataService>()
                .CreateEntityAsync(recordFactory(currentObject, stash), CancellationToken.None);
            if (result.IsT1) throw result.AsT1;
            if (!result.AsT0.Success)
            {
                throw new InvalidOperationException("Failed to create Business Central record.", result.AsT0.Exception);
            }

            var resultingRecord = result.AsT0.ResultingEntity;
            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = resultingRecord;
            return new ScenarioActionResult { CurrentObject = resultingRecord, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent UpdateRecord<TBusinessCentralEntity>(
        string fromStash,
        Func<TBusinessCentralEntity, TBusinessCentralEntity> modifier,
        string? stashTo = null)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var source = stash.GetValueOrDefault(fromStash).ToObject<TBusinessCentralEntity>()
                ?? throw new InvalidOperationException($"Business Central record '{fromStash}' was not found.");
            var update = modifier(source);
            update.Id ??= source.Id;
            update.ETag ??= source.ETag;
            var result = await AgentServices.GetRequiredService<IBusinessCentralODataService>()
                .UpdateEntityAsync(update, CancellationToken.None);
            if (!result.Success) throw new InvalidOperationException("Failed to update Business Central record.", result.Exception);

            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = result.ResultingEntity;
            return new ScenarioActionResult { CurrentObject = result.ResultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent MergeRecord<TBusinessCentralEntity, TDataHubEntity>(
        string fromStash,
        string? stashTo = null,
        bool forceUpdate = false)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var source = stash.GetValueOrDefault(fromStash).ToObject<TBusinessCentralEntity>()
                ?? throw new InvalidOperationException($"Business Central record '{fromStash}' was not found.");
            var response = await SendUsingMediator<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>(
                MergeSpecific<TBusinessCentralEntity, TDataHubEntity>([source.Id!], forceUpdate));
            var mergeResult = response.Results.SingleOrDefault()
                ?? throw new InvalidOperationException("Business Central merge did not return a result.");
            if (mergeResult.MergeOutcome == MergeOutcomes.MergeFailed)
            {
                throw new InvalidOperationException(mergeResult.FailureReason ?? "Business Central merge failed.");
            }

            var dataHubResponse = await AgentServices.GetRequiredService<IDataHubClient>()
                .PostRequestAsync<GetDataHubEntityRequest, GetDataHubEntityResponse>(new GetDataHubEntityRequest
                {
                    EntityType = mergeResult.DataHubEntityType,
                    EntityId = mergeResult.DataHubEntityId
                }, CancellationToken.None);
            var resultingEntity = dataHubResponse.Entity.ToObject<TDataHubEntity>()
                ?? throw new InvalidOperationException("The merged Data Hub record could not be loaded.");
            if (!string.IsNullOrWhiteSpace(stashTo))
            {
                stash[stashTo] = resultingEntity;
                stash[$"{stashTo}_mergeResult"] = mergeResult;
            }
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent MergeUpdated<TBusinessCentralEntity, TDataHubEntity>(
        DateTime? fromDateTime = null,
        int batchSize = 500,
        string? stashTo = null)
        where TBusinessCentralEntity : BusinessCentralDocument, IBusinessCentralIncrementalEntity
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var response = await SendUsingMediator<NullResponse>(new MergeUpdatedBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>
            {
                CorrelationId = InstanceId,
                FromDateTime = fromDateTime,
                BatchSize = batchSize
            });
            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = response;
            return new ScenarioActionResult { CurrentObject = response, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent SyncUpdated<TDataHubEntity, TBusinessCentralEntity>(
        int batchSize = 500,
        string? stashTo = null)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var response = await SendUsingMediator<NullResponse>(new SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>
            {
                CorrelationId = InstanceId,
                BatchSize = batchSize
            });
            if (!string.IsNullOrWhiteSpace(stashTo)) stash[stashTo] = response;
            return new ScenarioActionResult { CurrentObject = response, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent GetMergeMarker<TBusinessCentralEntity>(string stashTo)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var response = await SendUsingMediator<GetBusinessCentralMergeMarkerResponse>(new GetBusinessCentralMergeMarkerRequest
            {
                EntityType = typeof(TBusinessCentralEntity).Name,
                DefaultValue = DateTimeOffset.MinValue.ToString("o")
            });
            stash[stashTo] = response.MergeMarker!;
            return new ScenarioActionResult { CurrentObject = response.MergeMarker!, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent SetMergeMarker<TBusinessCentralEntity>(DateTimeOffset value, string stashTo)
        where TBusinessCentralEntity : BusinessCentralDocument
        => SetMergeMarker<TBusinessCentralEntity>((_, _) => value, stashTo);

    public BusinessCentralAgent SetMergeMarker<TBusinessCentralEntity>(
        Func<object, Dictionary<string, object?>, DateTimeOffset> valueFactory,
        string stashTo)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var value = valueFactory(currentObject, stash);
            var current = await SendUsingMediator<GetBusinessCentralMergeMarkerResponse>(
                new GetBusinessCentralMergeMarkerRequest
                {
                    EntityType = typeof(TBusinessCentralEntity).Name,
                    DefaultValue = value.ToString("o")
                });
            var response = await SendUsingMediator<UpdateBusinessCentralMergeMarkerResponse>(
                new UpdateBusinessCentralMergeMarkerRequest
                {
                    Marker = current.MergeMarker!,
                    NewValue = value.ToString("o")
                });
            var marker = response.ResultingMergeMarker
                ?? throw new InvalidOperationException("Data Hub did not return the seeded Business Central merge marker.");
            stash[stashTo] = marker;
            return new ScenarioActionResult { CurrentObject = marker, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent GetSyncMarker<TDataHubEntity>(string stashTo)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var response = await SendUsingMediator<GetBusinessCentralSyncMarkerResponse>(new GetBusinessCentralSyncMarkerRequest
            {
                EntityType = typeof(TDataHubEntity).Name,
                DefaultValue = DateTimeOffset.MinValue.ToString("o")
            });
            stash[stashTo] = response.SyncMarker;
            return new ScenarioActionResult { CurrentObject = response.SyncMarker, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent SetSyncMarker<TDataHubEntity>(DateTimeOffset value, string stashTo)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var current = await SendUsingMediator<GetBusinessCentralSyncMarkerResponse>(
                new GetBusinessCentralSyncMarkerRequest
                {
                    EntityType = typeof(TDataHubEntity).Name,
                    DefaultValue = value.ToString("o")
                });
            var response = await SendUsingMediator<UpdateBusinessCentralSyncMarkerResponse>(
                new UpdateBusinessCentralSyncMarkerRequest
                {
                    Marker = current.SyncMarker,
                    NewValue = value.ToString("o")
                });
            var marker = response.ResultingSyncMarker
                ?? throw new InvalidOperationException("Data Hub did not return the seeded Business Central sync marker.");
            stash[stashTo] = marker;
            return new ScenarioActionResult { CurrentObject = marker, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent SyncRecord<TDataHubEntity, TBusinessCentralEntity>(
        string fromStash,
        string stashTo,
        bool throwOnFailure = true)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            if (!stash.TryGetValue(fromStash, out var value) || value is null)
            {
                throw new InvalidOperationException($"SyncRecord: '{fromStash}' was not found in the scenario stash.");
            }

            var entity = value.ToObject<TDataHubEntity>()
                ?? throw new InvalidOperationException($"SyncRecord: '{fromStash}' is not a {typeof(TDataHubEntity).Name}.");
            var response = await SendUsingMediator<ProcessDataHubEntitySyncResponse>(
                SyncSpecific<TDataHubEntity, TBusinessCentralEntity>([entity.id]));
            var syncResult = response.Results.SingleOrDefault()
                ?? throw new InvalidOperationException("Business Central sync did not return a result.");

            stash[$"{stashTo}_syncResult"] = syncResult;
            if (SyncOutcomes.IsFailure(syncResult.SyncOutcome))
            {
                if (throwOnFailure)
                {
                    throw new InvalidOperationException(syncResult.FailureReason ?? "Business Central sync failed.");
                }

                return new ScenarioActionResult { CurrentObject = syncResult, Outputs = stash };
            }

            if (!Guid.TryParse(syncResult.SourceEntityId, out var sourceEntityId))
            {
                throw new InvalidOperationException("Business Central sync did not return a valid source entity id.");
            }

            var resultingEntity = await GetEntity<TBusinessCentralEntity>(sourceEntityId);
            stash[stashTo] = resultingEntity;

            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent SyncParentScopedRecord<TDataHubEntity, TBusinessCentralEntity>(
        string fromStash,
        string parentFromStash,
        string stashTo,
        bool throwOnFailure = true)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            if (!stash.TryGetValue(fromStash, out var value) || value is null)
            {
                throw new InvalidOperationException(
                    $"SyncParentScopedRecord: '{fromStash}' was not found in the scenario stash.");
            }

            var entity = value.ToObject<TDataHubEntity>()
                ?? throw new InvalidOperationException(
                    $"SyncParentScopedRecord: '{fromStash}' is not a {typeof(TDataHubEntity).Name}.");
            var parent = stash.GetValueOrDefault(parentFromStash).ToObject<BusinessCentralDocument>();
            if (parent is null || !Guid.TryParse(parent.Id, out var parentId))
            {
                throw new InvalidOperationException(
                    $"SyncParentScopedRecord: '{parentFromStash}' is not a Business Central record with a valid id.");
            }

            var response = await SendUsingMediator<ProcessDataHubEntitySyncResponse>(
                SyncSpecific<TDataHubEntity, TBusinessCentralEntity>([entity.id]));
            var syncResult = response.Results.SingleOrDefault()
                ?? throw new InvalidOperationException("Business Central sync did not return a result.");

            stash[$"{stashTo}_syncResult"] = syncResult;
            if (SyncOutcomes.IsFailure(syncResult.SyncOutcome))
            {
                if (throwOnFailure)
                {
                    throw new InvalidOperationException(syncResult.FailureReason ?? "Business Central sync failed.");
                }

                return new ScenarioActionResult { CurrentObject = syncResult, Outputs = stash };
            }

            if (!Guid.TryParse(syncResult.SourceEntityId, out var sourceEntityId))
            {
                throw new InvalidOperationException("Business Central sync did not return a valid source entity id.");
            }

            var resultingEntity = await GetParentScopedEntity<TBusinessCentralEntity>(parentId, sourceEntityId);
            stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent SyncRecords<TDataHubEntity, TBusinessCentralEntity>(
        string fromStash,
        string stashTo,
        bool throwOnFailure = false)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var entities = stash.GetValueOrDefault(fromStash).ToObject<List<TDataHubEntity>>()
                ?? throw new InvalidOperationException($"Data Hub records '{fromStash}' were not found.");
            var response = await SendUsingMediator<ProcessDataHubEntitySyncResponse>(
                SyncSpecific<TDataHubEntity, TBusinessCentralEntity>(entities.Select(entity => entity.id)));
            stash[stashTo] = response.Results;
            if (throwOnFailure && response.Results.Any(result => SyncOutcomes.IsFailure(result.SyncOutcome)))
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine,
                    response.Results.Where(result => SyncOutcomes.IsFailure(result.SyncOutcome))
                        .Select(result => result.FailureReason ?? "Business Central sync failed.")));
            }
            return new ScenarioActionResult { CurrentObject = response.Results, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent GetRecordFromStash<TBusinessCentralEntity>(string fromStash, string stashTo)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var source = stash[fromStash].ToObject<TBusinessCentralEntity>()
                ?? throw new InvalidOperationException($"Business Central record '{fromStash}' was not found in the scenario stash.");
            if (!Guid.TryParse(source.Id, out var sourceEntityId))
            {
                throw new InvalidOperationException($"Business Central record '{fromStash}' has no valid id.");
            }

            var resultingEntity = await GetEntity<TBusinessCentralEntity>(sourceEntityId);
            stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent GetTrackedDataHubEntity<TBusinessCentralEntity, TDataHubEntity>(
        string fromStash,
        string stashTo)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var source = stash[fromStash].ToObject<TBusinessCentralEntity>()
                ?? throw new InvalidOperationException($"Business Central record '{fromStash}' was not found in the scenario stash.");
            var dataHubClient = AgentServices.GetRequiredService<IDataHubClient>();
            var dataSource = AgentServices.GetRequiredService<IOptions<BusinessCentralAgentOptions>>().Value.DataSource
                ?? throw new InvalidOperationException("BusinessCentralAgentOptions.DataSource is required.");
            var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
            TDataHubEntity? trackedEntity = null;

            do
            {
                var response = await dataHubClient.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(
                    new GetTrackedEntitiesRequest
                    {
                        DataSource = dataSource,
                        EntityType = typeof(TBusinessCentralEntity).Name,
                        EntityIds = [source.Id!]
                    },
                    CancellationToken.None);
                trackedEntity = response.Results.SingleOrDefault()?.Data?.ToObject<TDataHubEntity>();
                if (trackedEntity is null) await Task.Delay(100);
            }
            while (trackedEntity is null && DateTimeOffset.UtcNow < timeoutAt);

            if (trackedEntity is null)
            {
                throw new InvalidOperationException(
                    $"No Data Hub tracking entry was found for {typeof(TBusinessCentralEntity).Name}/{source.Id}.");
            }

            stash[stashTo] = trackedEntity;
            return new ScenarioActionResult { CurrentObject = trackedEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent GetDataHubEntityByAlternateKey<TBusinessCentralEntity, TDataHubEntity>(
        string fromStash,
        string stashTo)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            var source = stash[fromStash].ToObject<TBusinessCentralEntity>()
                ?? throw new InvalidOperationException($"Business Central record '{fromStash}' was not found in the scenario stash.");
            var dataHubClient = AgentServices.GetRequiredService<IDataHubClient>();
            var dataSource = AgentServices.GetRequiredService<IOptions<BusinessCentralAgentOptions>>().Value.DataSource
                ?? throw new InvalidOperationException("BusinessCentralAgentOptions.DataSource is required.");
            var response = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByAltKeyRequest, GetDataHubEntitiesByAltKeyResponse>(
                new GetDataHubEntitiesByAltKeyRequest
                {
                    EntityType = typeof(TDataHubEntity).Name,
                    AlternateKeys = [BusinessCentralAlternateKey<TBusinessCentralEntity>(dataSource, source.Id!)]
                },
                CancellationToken.None);

            var matchingEntities = response.Results
                .Select(result => result.ToObject<TDataHubEntity>())
                .Where(entity => entity is not null)
                .Cast<TDataHubEntity>()
                .ToList();
            var resultingEntity = matchingEntities.Count switch
            {
                1 => matchingEntities[0],
                0 => throw new InvalidOperationException(
                    $"No Data Hub {typeof(TDataHubEntity).Name} was found for {typeof(TBusinessCentralEntity).Name}/{source.Id}."),
                _ => throw new InvalidOperationException(
                    $"More than one Data Hub {typeof(TDataHubEntity).Name} was found for {typeof(TBusinessCentralEntity).Name}/{source.Id}.")
            };

            stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent DeleteDataHubEntityByAlternateKey<TBusinessCentralEntity, TDataHubEntity>(
        string fromStash)
        where TBusinessCentralEntity : BusinessCentralDocument
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            try
            {
                var source = stash.GetValueOrDefault(fromStash).ToObject<TBusinessCentralEntity>();
                if (source?.Id is not null)
                {
                    var dataHubClient = AgentServices.GetRequiredService<IDataHubClient>();
                    var dataSource = AgentServices.GetRequiredService<IOptions<BusinessCentralAgentOptions>>().Value.DataSource
                        ?? throw new InvalidOperationException("BusinessCentralAgentOptions.DataSource is required.");
                    var response = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByAltKeyRequest, GetDataHubEntitiesByAltKeyResponse>(
                        new GetDataHubEntitiesByAltKeyRequest
                        {
                            EntityType = typeof(TDataHubEntity).Name,
                            AlternateKeys = [BusinessCentralAlternateKey<TBusinessCentralEntity>(dataSource, source.Id)]
                        },
                        CancellationToken.None);
                    var entityIds = response.Results
                        .Select(result => result.ToObject<TDataHubEntity>()?.id)
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Cast<string>()
                        .ToList();
                    if (entityIds.Count > 0)
                    {
                        await dataHubClient.PostRequestAsync<DeleteDataHubEntitiesRequest, DeleteDataHubEntitiesResponse>(
                            new DeleteDataHubEntitiesRequest
                            {
                                EntityType = typeof(TDataHubEntity).Name,
                                EntityIds = entityIds,
                                IncludeTrackingEntries = true
                            },
                            CancellationToken.None);
                    }
                }
            }
            catch
            {
                // Finally cleanup is best-effort and restricted to the source record's alternate key.
            }

            return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent DeleteRecord<TBusinessCentralEntity>(
        string fromStash,
        bool throwOnFailure = false)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            try
            {
                stash.TryGetValue(fromStash, out var value);
                var capturedEntity = value switch
                {
                    TBusinessCentralEntity entity => entity,
                    string => null,
                    null => null,
                    _ => value.ToObject<TBusinessCentralEntity>()
                };
                var entityId = capturedEntity?.Id ?? value as string;

                if (string.IsNullOrWhiteSpace(entityId) &&
                    stash.TryGetValue($"{fromStash}_syncResult", out var resultValue))
                {
                    entityId = resultValue.ToObject<SyncEntityResult>()?.SourceEntityId;
                }

                if (!string.IsNullOrWhiteSpace(entityId))
                {
                    var service = AgentServices.GetRequiredService<IBusinessCentralODataService>();
                    if (typeof(TBusinessCentralEntity) == typeof(BusinessCentralPurchaseInvoice))
                    {
                        if (!Guid.TryParse(entityId, out var purchaseInvoiceId))
                        {
                            throw new InvalidOperationException(
                                $"Cannot safely delete {typeof(TBusinessCentralEntity).Name}/{entityId} " +
                                "because its id is not a GUID.");
                        }

                        await DeletePurchaseInvoiceSafely(service, purchaseInvoiceId);
                        return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
                    }

                    if (throwOnFailure)
                    {
                        if (!Guid.TryParse(entityId, out var id))
                        {
                            throw new InvalidOperationException(
                                $"Cannot safely delete {typeof(TBusinessCentralEntity).Name}/{entityId} " +
                                "because its id is not a GUID.");
                        }

                        var currentResponse = await service.GetEntityAsync<TBusinessCentralEntity>(
                            id,
                            CancellationToken.None);
                        if (currentResponse.IsT2) throw currentResponse.AsT2;
                        if (currentResponse.IsT1)
                        {
                            using var readResponse = currentResponse.AsT1;
                            if (readResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
                            }

                            var body = await readResponse.Content.ReadAsStringAsync();
                            throw new HttpRequestException(
                                $"Business Central read before deletion for " +
                                $"{typeof(TBusinessCentralEntity).Name}/{entityId} failed with " +
                                $"{(int)readResponse.StatusCode} {readResponse.ReasonPhrase}: {body}");
                        }

                        capturedEntity = currentResponse.AsT0;
                    }

                    var response = capturedEntity is not null
                        ? await service.DeleteEntityAsync(capturedEntity, CancellationToken.None)
                        : await service.DeleteEntityAsync<TBusinessCentralEntity>(
                            entityId,
                            CancellationToken.None);
                    if (response.IsT2) throw response.AsT2;
                    if (response.IsT1)
                    {
                        using var httpResponse = response.AsT1;
                        if (throwOnFailure)
                        {
                            var body = await httpResponse.Content.ReadAsStringAsync();
                            throw new HttpRequestException(
                                $"Business Central delete for {typeof(TBusinessCentralEntity).Name}/{entityId} " +
                                $"failed with {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
                        }
                    }
                    else if (throwOnFailure)
                    {
                        await VerifyRecordDeleted<TBusinessCentralEntity>(
                            entityId,
                            allowSafeInvoiceAggregateCleanup: true);
                    }
                }
            }
            catch when (!throwOnFailure)
            {
                // Finally cleanup is best-effort and restricted to the captured test record id.
            }

            return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    public BusinessCentralAgent VerifyRecordDeleted<TBusinessCentralEntity>(string fromStash)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        async Task<ScenarioActionResult> Action(object currentObject, Dictionary<string, object?> stash)
        {
            stash.TryGetValue(fromStash, out var value);
            var capturedEntity = value switch
            {
                TBusinessCentralEntity entity => entity,
                string => null,
                null => null,
                _ => value.ToObject<TBusinessCentralEntity>()
            };
            var entityId = capturedEntity?.Id ?? value as string;

            if (string.IsNullOrWhiteSpace(entityId) &&
                stash.TryGetValue($"{fromStash}_syncResult", out var resultValue))
            {
                entityId = resultValue.ToObject<SyncEntityResult>()?.SourceEntityId;
            }

            if (!string.IsNullOrWhiteSpace(entityId))
            {
                await VerifyRecordDeleted<TBusinessCentralEntity>(
                    entityId,
                    allowSafeInvoiceAggregateCleanup: false);
            }

            return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(Action);
        return this;
    }

    private async Task VerifyRecordDeleted<TBusinessCentralEntity>(
        string entityId,
        bool allowSafeInvoiceAggregateCleanup = true)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        if (!Guid.TryParse(entityId, out var id))
        {
            throw new InvalidOperationException(
                $"Cannot verify deletion of {typeof(TBusinessCentralEntity).Name}/{entityId} because its id is not a GUID.");
        }

        var service = AgentServices.GetRequiredService<IBusinessCentralODataService>();
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
        TBusinessCentralEntity? retainedRecord = null;
        do
        {
            var response = await service.GetEntityAsync<TBusinessCentralEntity>(id, CancellationToken.None);
            if (response.IsT2) throw response.AsT2;
            if (response.IsT1)
            {
                using var httpResponse = response.AsT1;
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) return;

                var body = await httpResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Business Central deletion verification for {typeof(TBusinessCentralEntity).Name}/{entityId} " +
                    $"failed with {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
            }

            retainedRecord = response.AsT0;

            if (allowSafeInvoiceAggregateCleanup &&
                retainedRecord is BusinessCentralSalesInvoice retainedInvoice &&
                await TryDeleteEmptyPostedTestInvoiceArtifact(service, retainedInvoice, id))
            {
                await VerifyRecordDeleted<TBusinessCentralEntity>(
                    entityId,
                    allowSafeInvoiceAggregateCleanup: false);
                return;
            }

            if (allowSafeInvoiceAggregateCleanup &&
                retainedRecord is BusinessCentralPurchaseInvoice)
            {
                await DeleteRetainedPurchaseInvoicePlaceholder(service, id);
                return;
            }

            if (DateTimeOffset.UtcNow < timeoutAt)
            {
                await Task.Delay(250, CancellationToken.None);
            }
        }
        while (DateTimeOffset.UtcNow < timeoutAt);

        var status = retainedRecord?.GetAttributes()
            .FirstOrDefault(attribute =>
                string.Equals(attribute.Key, "status", StringComparison.OrdinalIgnoreCase))
            .Value?.ToString();
        var statusDetail = string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : $" Its current status is '{status}'.";

        throw new InvalidOperationException(
            $"Business Central acknowledged deletion of {typeof(TBusinessCentralEntity).Name}/{entityId}, " +
            "but the record remained readable after five seconds." + statusDetail);
    }

    private static async Task<bool> TryDeleteEmptyPostedTestInvoiceArtifact(
        IBusinessCentralODataService service,
        BusinessCentralSalesInvoice invoice,
        Guid invoiceId)
    {
        var lineResponse = await service.GetEntitiesAsync<BusinessCentralSalesInvoiceLine>(
            filter: $"documentId eq {invoiceId}",
            cancellationToken: CancellationToken.None);
        if (lineResponse.IsT2) throw lineResponse.AsT2;
        if (lineResponse.IsT1)
        {
            using var httpResponse = lineResponse.AsT1;
            var body = await httpResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Business Central line check before posted test-invoice cleanup failed with " +
                $"{(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
        }

        if (!BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(
                invoice,
                lineResponse.AsT0.Value))
        {
            return false;
        }

        var deleteResponse = await service.DeleteEntityAsync(invoice, CancellationToken.None);
        if (deleteResponse.IsT2) throw deleteResponse.AsT2;
        if (deleteResponse.IsT1)
        {
            using var httpResponse = deleteResponse.AsT1;
            var body = await httpResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Business Central cleanup of empty posted test invoice {invoice.Id} failed with " +
                $"{(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
        }

        return true;
    }

    private async Task DeletePurchaseInvoiceSafely(
        IBusinessCentralODataService service,
        Guid invoiceId)
    {
        var cleanup = await DeleteCurrentPurchaseInvoiceAsync(
            service,
            invoiceId,
            allowDraftDelete: true);
        if (cleanup.Disposition == BusinessCentralPurchaseInvoiceCleanupDisposition.NotFound)
        {
            return;
        }

        await VerifyRecordDeleted<BusinessCentralPurchaseInvoice>(
            invoiceId.ToString(),
            allowSafeInvoiceAggregateCleanup:
                cleanup.Disposition == BusinessCentralPurchaseInvoiceCleanupDisposition.DraftDeleted);
        foreach (var placeholderLine in cleanup.CapturedLines)
        {
            await VerifyRecordDeleted<BusinessCentralPurchaseInvoiceLine>(
                placeholderLine.Id!,
                allowSafeInvoiceAggregateCleanup: false);
        }
    }

    private async Task DeleteRetainedPurchaseInvoicePlaceholder(
        IBusinessCentralODataService service,
        Guid invoiceId)
    {
        var cleanup = await DeleteCurrentPurchaseInvoiceAsync(
            service,
            invoiceId,
            allowDraftDelete: false);
        if (cleanup.Disposition == BusinessCentralPurchaseInvoiceCleanupDisposition.NotFound)
        {
            return;
        }

        if (cleanup.Disposition !=
            BusinessCentralPurchaseInvoiceCleanupDisposition.NoSeriesPlaceholderDeleted)
        {
            throw new InvalidOperationException(
                $"Purchase invoice {invoiceId} changed unexpectedly during guarded cleanup.");
        }

        await VerifyRecordDeleted<BusinessCentralPurchaseInvoice>(
            invoiceId.ToString(),
            allowSafeInvoiceAggregateCleanup: false);
        foreach (var placeholderLine in cleanup.CapturedLines)
        {
            await VerifyRecordDeleted<BusinessCentralPurchaseInvoiceLine>(
                placeholderLine.Id!,
                allowSafeInvoiceAggregateCleanup: false);
        }

    }

    private Task<BusinessCentralPurchaseInvoiceCleanupResult> DeleteCurrentPurchaseInvoiceAsync(
        IBusinessCentralODataService service,
        Guid invoiceId,
        bool allowDraftDelete) =>
        BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            invoiceId,
            (id, cancellationToken) => ReadPurchaseInvoiceForCleanupAsync(
                service,
                id,
                cancellationToken),
            (id, cancellationToken) => ReadCompletePurchaseInvoiceLinesForCleanupAsync(
                service,
                id,
                cancellationToken),
            (invoice, cancellationToken) => DeletePurchaseInvoiceWithExactETagAsync(
                service,
                invoice,
                cancellationToken),
            allowDraftDelete,
            CancellationToken.None);

    private static async Task<BusinessCentralPurchaseInvoice?> ReadPurchaseInvoiceForCleanupAsync(
        IBusinessCentralODataService service,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var response = await service.GetEntityAsync<BusinessCentralPurchaseInvoice>(
            invoiceId,
            cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Business Central purchase-invoice cleanup read failed with " +
                $"{(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
        }

        return response.AsT0;
    }

    private static async Task<BusinessCentralPurchaseInvoiceLineSnapshot>
        ReadCompletePurchaseInvoiceLinesForCleanupAsync(
            IBusinessCentralODataService service,
            Guid invoiceId,
            CancellationToken cancellationToken)
    {
        var response = await service.GetEntitiesAsync<BusinessCentralPurchaseInvoiceLine>(
            filter: $"documentId eq {invoiceId}",
            order: "sequence",
            cancellationToken: cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Business Central complete purchase-invoice line read failed with " +
                $"{(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
        }

        return new BusinessCentralPurchaseInvoiceLineSnapshot(
            response.AsT0.Count,
            response.AsT0.Value);
    }

    private static async Task DeletePurchaseInvoiceWithExactETagAsync(
        IBusinessCentralODataService service,
        BusinessCentralPurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(invoice.ETag))
        {
            throw new InvalidOperationException(
                $"Purchase invoice {invoice.Id} has no ETag. Cleanup will not use a wildcard If-Match value.");
        }

        var deleteResponse = await service.DeleteEntityAsync(invoice, cancellationToken);
        if (deleteResponse.IsT2) throw deleteResponse.AsT2;
        if (deleteResponse.IsT1)
        {
            using var httpResponse = deleteResponse.AsT1;
            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var guidance = BusinessCentralIntegrationFailureDiagnostics
                .IsPostedPurchaseDocumentDeletionBlocked(body)
                ? BusinessCentralIntegrationFailureDiagnostics.PostedPurchaseDocumentDeletionGuidance
                : BusinessCentralIntegrationFailureDiagnostics
                    .IsPurchaseInvoicePlaceholderPermissionBlocked(body)
                    ? BusinessCentralIntegrationFailureDiagnostics
                        .PurchaseInvoicePlaceholderPermissionGuidance
                    : "Inspect the isolated Business Central company and the captured DHIT record.";
            throw new HttpRequestException(
                $"Business Central cleanup of purchase invoice {invoice.Id} with its exact ETag " +
                $"failed with {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}. " +
                $"{guidance} Response: {body}",
                null,
                httpResponse.StatusCode);
        }
    }

    private async Task<TResponse> SendUsingMediator<TResponse>(IRequest<TResponse> request)
    {
        var result = await AgentServices.GetRequiredService<IMediator>()
            .TrySend<TResponse>(request, CancellationToken.None);
        if (result.Item2 is not null)
        {
            throw new InvalidOperationException(
                $"Mediator request '{request.GetType().Name}' failed: {result.Item2}",
                result.Item2);
        }
        return result.Item1 ?? throw new InvalidOperationException("Mediator returned no response.");
    }

    private static AlternateKey BusinessCentralAlternateKey<TBusinessCentralEntity>(string dataSource, string sourceEntityId)
        where TBusinessCentralEntity : BusinessCentralDocument => new()
        {
            Key = $"{dataSource}.{typeof(TBusinessCentralEntity).Name}".ToLowerInvariant(),
            Value = sourceEntityId
        };

    private async Task<TBusinessCentralEntity> GetEntity<TBusinessCentralEntity>(Guid id)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var response = await AgentServices.GetRequiredService<IBusinessCentralODataService>()
            .GetEntityAsync<TBusinessCentralEntity>(id, CancellationToken.None);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            var body = await httpResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Business Central read for {typeof(TBusinessCentralEntity).Name}/{id} failed with {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}: {body}");
        }

        return response.AsT0
            ?? throw new InvalidOperationException($"Business Central record {typeof(TBusinessCentralEntity).Name}/{id} was not found.");
    }

    private async Task<TBusinessCentralEntity> GetParentScopedEntity<TBusinessCentralEntity>(Guid parentId, Guid id)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var response = await AgentServices.GetRequiredService<IBusinessCentralODataService>()
            .GetEntityAsync<TBusinessCentralEntity>(parentId, id, CancellationToken.None);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            var body = await httpResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Business Central parent-scoped read for {typeof(TBusinessCentralEntity).Name}/" +
                $"{parentId}/{id} failed with {(int)httpResponse.StatusCode} " +
                $"{httpResponse.ReasonPhrase}: {body}");
        }

        return response.AsT0
            ?? throw new InvalidOperationException(
                $"Business Central parent-scoped record {typeof(TBusinessCentralEntity).Name}/" +
                $"{parentId}/{id} was not found.");
    }
}
