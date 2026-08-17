using System.Reflection;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

public static class BusinessCentralRouteMetadata
{
    public static void EnsureParentRouteValueUnchanged<TBusinessCentralDocument>(
        TBusinessCentralDocument current,
        TBusinessCentralDocument requested)
        where TBusinessCentralDocument : BusinessCentralDocument
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(requested);

        var entityType = typeof(TBusinessCentralDocument);
        var parentUrl = entityType.GetCustomAttribute<BusinessCentralParentUrlAttribute>(inherit: true);
        if (parentUrl is null)
        {
            return;
        }

        var parentIdProperty = GetParentIdProperty(entityType, parentUrl);
        var currentParentId = parentIdProperty.GetValue(current);
        var requestedParentId = parentIdProperty.GetValue(requested);

        // A mapper may omit the parent from an update that changes only ordinary fields. The
        // existing value will still be copied to the mutation solely to construct the route.
        if (requestedParentId is null)
        {
            return;
        }

        if (currentParentId is null || !Equals(currentParentId, requestedParentId))
        {
            throw new InvalidOperationException(
                $"Business Central {entityType.Name} cannot be moved between parent records. " +
                $"Its existing '{parentUrl.ParentIdPropertyName}' is '{currentParentId ?? "<missing>"}', " +
                $"but Data Hub requested '{requestedParentId}'. Create a new child record instead.");
        }
    }

    public static void CopyParentRouteValue<TBusinessCentralDocument>(
        TBusinessCentralDocument source,
        TBusinessCentralDocument target)
        where TBusinessCentralDocument : BusinessCentralDocument
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var entityType = typeof(TBusinessCentralDocument);
        var parentUrl = entityType.GetCustomAttribute<BusinessCentralParentUrlAttribute>(inherit: true);
        if (parentUrl is null)
        {
            return;
        }

        var parentIdProperty = GetParentIdProperty(entityType, parentUrl);

        parentIdProperty.SetValue(target, parentIdProperty.GetValue(source));
    }

    private static PropertyInfo GetParentIdProperty(
        Type entityType,
        BusinessCentralParentUrlAttribute parentUrl)
    {
        var parentIdProperty = entityType.GetProperty(
            parentUrl.ParentIdPropertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (parentIdProperty is null || !parentIdProperty.CanRead || !parentIdProperty.CanWrite)
        {
            throw new InvalidOperationException(
                $"Business Central parent route for {entityType.Name} requires a readable and writable property " +
                $"named '{parentUrl.ParentIdPropertyName}'.");
        }

        return parentIdProperty;
    }
}
