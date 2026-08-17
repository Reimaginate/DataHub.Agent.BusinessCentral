using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>
/// Minimal base contract for a Business Central API entity.
/// </summary>
public abstract class BusinessCentralDocument
{
    private readonly Dictionary<string, object?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("@odata.etag")]
    public string? ETag { get; set; }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public event System.ComponentModel.PropertyChangingEventHandler? PropertyChanging;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    protected void OnPropertyChanging(string propertyName)
    {
        PropertyChanging?.Invoke(this, new System.ComponentModel.PropertyChangingEventArgs(propertyName));
    }

    private object? GetAttributeValue(string attributeLogicalName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeLogicalName);
        return _attributes.GetValueOrDefault(attributeLogicalName);
    }

    protected T? GetAttributeValue<T>(string attributeLogicalName)
    {
        var attributeValue = GetAttributeValue(attributeLogicalName);
        return attributeValue is null ? default : (T)attributeValue;
    }

    protected void SetAttributeValue(string attributeLogicalName, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeLogicalName);
        _attributes[attributeLogicalName] = value;
    }

    protected void SetWithNotification(string attributeName, object? value)
    {
        OnPropertyChanging(attributeName);
        SetAttributeValue(attributeName, value);
        OnPropertyChanged(attributeName);
    }

    public IReadOnlyDictionary<string, object?> GetAttributes()
    {
        return _attributes;
    }
}
