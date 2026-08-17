namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class BusinessCentralApiRouteAttribute(string apiRoute) : Attribute
{
    public string ApiRoute { get; } = apiRoute;
}
