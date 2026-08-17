namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class BusinessCentralUrlAttribute(string url) : Attribute
{
    public string Url { get; } = url;
}
