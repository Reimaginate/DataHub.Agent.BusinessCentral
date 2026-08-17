namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>
/// Selects an optional custom Business Central reservation endpoint used to create a uniquely
/// correlated skeleton before the standard API applies the full mapped entity.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class BusinessCentralCreateReservationAttribute(
    string entitySet,
    string documentType) : Attribute
{
    public string EntitySet { get; } = entitySet;
    public string DocumentType { get; } = documentType;
}
