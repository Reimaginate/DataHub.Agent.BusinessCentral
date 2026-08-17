namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>
/// Identifies a writable Business Central field that participates in the key used to recover an
/// ambiguous create. Apply the attribute to every property required to uniquely identify the
/// record; recovery is disabled when any attributed value is missing.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class BusinessCentralCreateRecoveryKeyAttribute(string fieldName) : Attribute
{
    public string FieldName { get; } = fieldName;
}
