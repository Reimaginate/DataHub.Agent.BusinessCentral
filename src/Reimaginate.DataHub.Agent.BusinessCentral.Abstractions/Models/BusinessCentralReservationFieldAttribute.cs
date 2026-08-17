namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>Includes a property in the custom create-reservation request.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BusinessCentralReservationFieldAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
