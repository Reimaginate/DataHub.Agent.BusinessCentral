namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>
/// Identifies the Business Central API property used for incremental retrieval.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class BusinessCentralLastModifiedAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; } = propertyName;
}
