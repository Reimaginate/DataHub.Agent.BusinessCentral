using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;

public class EmptyStringJsonConverter : JsonConverter<string>
{
    public override bool HandleNull => true;

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var content = reader.GetString();
        return string.IsNullOrEmpty(content) ? null : content;
    }

    public override void Write(Utf8JsonWriter writer, string? stringValue, JsonSerializerOptions options)
    {
        writer.WriteStringValue(string.IsNullOrEmpty(stringValue) ? string.Empty : stringValue);
    }
}
