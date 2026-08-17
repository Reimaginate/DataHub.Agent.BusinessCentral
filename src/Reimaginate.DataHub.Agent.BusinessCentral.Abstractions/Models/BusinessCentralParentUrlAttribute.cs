namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class BusinessCentralParentUrlAttribute(
    string parentUrl,
    string parentIdPropertyName) : Attribute
{
    public string ParentUrl { get; } = parentUrl;

    public string ParentIdPropertyName { get; } = parentIdPropertyName;
}
