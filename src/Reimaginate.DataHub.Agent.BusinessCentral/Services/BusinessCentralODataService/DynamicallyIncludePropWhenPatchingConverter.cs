using System.Text.Json;
using System.Text.Json.Serialization;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;

public sealed class DynamicallyIncludePropWhenPatchingConverter<T> : JsonConverter<T>
{
    private readonly HashSet<string> _propertiesToInclude;

    public DynamicallyIncludePropWhenPatchingConverter(HashSet<string> propertiesToInclude)
    {
        _propertiesToInclude = propertiesToInclude;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions? options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null) return;

        writer.WriteStartObject();

        var typeProps = value.GetType().GetProperties();
        foreach (var property in typeProps)
        {
            if (!_propertiesToInclude.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) continue;

            var propertyName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            var jsonPropertyNameAttr = property.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                .OfType<JsonPropertyNameAttribute>().FirstOrDefault();
            if (jsonPropertyNameAttr != null)
            {
                propertyName = jsonPropertyNameAttr.Name;
            }

            var propertyValue = property.GetValue(value);

            writer.WritePropertyName(propertyName);
            if (property.GetCustomAttributes(typeof(BusinessCentralDateAttribute), true).Length > 0 &&
                propertyValue is null or "")
            {
                // AL Date fields are non-nullable Edm.Date values. Business Central exposes its
                // undefined date (0D) as the ISO date below; an empty string is not an Edm.Date.
                writer.WriteStringValue(BusinessCentralDateAttribute.UndefinedDateValue);
            }
            else if (propertyValue is null && property.PropertyType == typeof(string))
            {
                // Business Central string fields are non-nullable Edm.String values. An empty
                // string is the supported representation for clearing them; the read converter
                // normalizes the returned empty string back to null for stable comparisons.
                writer.WriteStringValue(string.Empty);
            }
            else
            {
                JsonSerializer.Serialize(writer, propertyValue, options);
            }
        }

        writer.WriteEndObject();
    }
}
