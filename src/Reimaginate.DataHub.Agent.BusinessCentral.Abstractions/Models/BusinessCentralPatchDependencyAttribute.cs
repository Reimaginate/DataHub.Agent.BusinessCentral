namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>
/// Ensures that a mapped Business Central property is included in a PATCH whenever
/// one of the named mapped attributes changes. This is used for values that
/// Business Central recalculates as a side effect of updating another field.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class BusinessCentralPatchDependencyAttribute : Attribute
{
    public BusinessCentralPatchDependencyAttribute(params string[] triggerAttributeNames)
    {
        ArgumentNullException.ThrowIfNull(triggerAttributeNames);
        if (triggerAttributeNames.Length == 0 || triggerAttributeNames.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one non-empty Business Central trigger attribute name is required.",
                nameof(triggerAttributeNames));
        }

        TriggerAttributeNames = triggerAttributeNames;
    }

    public IReadOnlyList<string> TriggerAttributeNames { get; }
}
