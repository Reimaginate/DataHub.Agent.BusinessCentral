using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public static class BusinessCentralMappingHelpers
{
    private const string DataSource = "BusinessCentral";

    public static string? ToBusinessCentralDate(DateTime? value)
    {
        return value.HasValue
            ? DateOnly.FromDateTime(value.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }

    public static DateTime? ToDataHubDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == BusinessCentralDateAttribute.UndefinedDateValue)
        {
            return null;
        }

        return DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .ToDateTime(TimeOnly.MinValue);
    }

    public static ExternalEntityReference? ToDataHubReference<TDataHubEntity, TBusinessCentralEntity>(Guid? id)
        where TDataHubEntity : DataHubEntity
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        return id.HasValue && id.Value != Guid.Empty
            ? new ExternalEntityReference
            {
                DataSource = DataSource,
                EntityType = typeof(TDataHubEntity).Name,
                SourceEntityType = typeof(TBusinessCentralEntity).Name,
                EntityId = id.Value.ToString()
            }
            : null;
    }

    public static Guid? ResolveBusinessCentralId<TDataHubEntity>(
        EntityReference? reference,
        string businessCentralEntityType,
        Dictionary<string, object>? cache)
        where TDataHubEntity : DataHubEntity
    {
        if (reference is null)
        {
            return null;
        }

        if (reference is ExternalEntityReference externalReference &&
            externalReference.DataSource.Equals(DataSource, StringComparison.OrdinalIgnoreCase) &&
            externalReference.SourceEntityType.Equals(businessCentralEntityType, StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(externalReference.EntityId, out var externalId))
        {
            return externalId;
        }

        if (cache is null ||
            !cache.TryGetValue(typeof(TDataHubEntity).Name, out var cachedEntities) ||
            cachedEntities is not IEnumerable<JObject> cachedObjects)
        {
            return null;
        }

        var matchingEntity = cachedObjects.FirstOrDefault(entity =>
            entity.Value<string>(nameof(DataHubEntity.id)) == reference.EntityId);
        var expectedKey = $"{DataSource}.{businessCentralEntityType}";
        var sourceId = matchingEntity?[nameof(DataHubEntity.alternateKeys)]?
            .Children<JObject>()
            .Where(key => string.Equals(
                key.Value<string>(nameof(AlternateKey.Key)) ?? key.Value<string>("key"),
                expectedKey,
                StringComparison.OrdinalIgnoreCase))
            .Select(key =>
                key.Value<string>(nameof(AlternateKey.Value)) ?? key.Value<string>("value"))
            .FirstOrDefault();

        return Guid.TryParse(sourceId, out var resolvedId) ? resolvedId : null;
    }

    public static Guid CreateStableCorrelationId(string entityType, string dataHubId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataHubId);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{entityType}:{dataHubId}"));
        var bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
