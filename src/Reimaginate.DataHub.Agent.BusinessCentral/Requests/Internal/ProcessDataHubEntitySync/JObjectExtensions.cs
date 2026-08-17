using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;

public static class JObjectExtensions
{
    public static string TryGetAlternateKeyValue(this JObject dataHubEntity, string key)
    {
        var alternateKeys = dataHubEntity.Value<JArray>(nameof(DataHubEntity.alternateKeys));
        var match = alternateKeys?.Children().FirstOrDefault(w => w.Value<string>(nameof(AlternateKey.Key)) == key);
        return match?.Value<string>(nameof(AlternateKey.Value));
    }

    public static List<JObject> TryGetSourceSystemAlternateKeys(this JObject dataHubEntity, string key)
    {
        var alternateKeys = dataHubEntity.Value<JArray>(nameof(DataHubEntity.alternateKeys));
        return alternateKeys?.Children()
            .Where(w => w.Value<string>(nameof(AlternateKey.Key))?.Split('.').FirstOrDefault() == key)
            .Select(s => (JObject)s)
            .ToList()
            ?? new List<JObject>();
    }
}