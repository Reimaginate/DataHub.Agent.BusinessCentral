using FluentAssertions;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Xunit;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralRouteMetadataTests
{
    [Fact(DisplayName = "Update deltas preserve parent-scoped route values")]
    [Trait("Category", "Unit")]
    public void CopiesParentRouteValueToUpdateDelta()
    {
        var documentId = Guid.NewGuid();
        var source = new BusinessCentralSalesInvoiceLine { DocumentId = documentId };
        var updateDelta = new BusinessCentralSalesInvoiceLine { Quantity = 2m };

        BusinessCentralRouteMetadata.CopyParentRouteValue(source, updateDelta);

        updateDelta.DocumentId.Should().Be(documentId);
        updateDelta.GetAttributes().Should().ContainKey("documentId");
    }

    [Fact(DisplayName = "Unscoped update deltas require no parent route value")]
    [Trait("Category", "Unit")]
    public void IgnoresEntitiesWithoutAParentRoute()
    {
        var source = new BusinessCentralCustomer { DisplayName = "Before" };
        var updateDelta = new BusinessCentralCustomer { DisplayName = "After" };

        var act = () => BusinessCentralRouteMetadata.CopyParentRouteValue(source, updateDelta);

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Parent-scoped updates reject moving a child to another parent")]
    [Trait("Category", "Unit")]
    public void RejectsChangedParentRouteValue()
    {
        var currentParentId = Guid.NewGuid();
        var requestedParentId = Guid.NewGuid();
        var current = new BusinessCentralSalesInvoiceLine { DocumentId = currentParentId };
        var requested = new BusinessCentralSalesInvoiceLine { DocumentId = requestedParentId };

        var act = () => BusinessCentralRouteMetadata.EnsureParentRouteValueUnchanged(
            current,
            requested);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be moved between parent records*")
            .WithMessage($"*{currentParentId}*")
            .WithMessage($"*{requestedParentId}*");
    }

    [Fact(DisplayName = "Parent-scoped updates allow the same or an omitted parent")]
    [Trait("Category", "Unit")]
    public void AllowsUnchangedOrOmittedParentRouteValue()
    {
        var parentId = Guid.NewGuid();
        var current = new BusinessCentralSalesInvoiceLine { DocumentId = parentId };
        var unchanged = new BusinessCentralSalesInvoiceLine { DocumentId = parentId };
        var omitted = new BusinessCentralSalesInvoiceLine { Quantity = 2m };

        var unchangedAct = () => BusinessCentralRouteMetadata.EnsureParentRouteValueUnchanged(
            current,
            unchanged);
        var omittedAct = () => BusinessCentralRouteMetadata.EnsureParentRouteValueUnchanged(
            current,
            omitted);

        unchangedAct.Should().NotThrow();
        omittedAct.Should().NotThrow();
    }
}
