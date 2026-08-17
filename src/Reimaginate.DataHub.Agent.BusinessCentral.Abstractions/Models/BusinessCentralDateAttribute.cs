using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>
/// Marks a string property whose Business Central OData type is <c>Edm.Date</c>.
/// Business Central represents its undefined AL date (<c>0D</c>) as
/// <c>0001-01-01</c> on the API wire.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BusinessCentralDateAttribute : JsonConverterAttribute
{
    public const string UndefinedDateValue = "0001-01-01";

    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        if (typeToConvert != typeof(string))
        {
            throw new InvalidOperationException(
                $"{nameof(BusinessCentralDateAttribute)} can only be applied to string properties.");
        }

        return new BusinessCentralDateJsonConverter();
    }

    private sealed class BusinessCentralDateJsonConverter : JsonConverter<string>
    {
        public override bool HandleNull => true;

        public override string? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var value = reader.GetString();
            return string.IsNullOrEmpty(value) ||
                   string.Equals(value, UndefinedDateValue, StringComparison.Ordinal)
                ? null
                : value;
        }

        public override void Write(
            Utf8JsonWriter writer,
            string? value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(string.IsNullOrEmpty(value) ? UndefinedDateValue : value);
        }
    }
}
