using FluentValidation;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker;

public class GetBusinessCentralMergeMarkerRequestValidator : AbstractValidator<GetBusinessCentralMergeMarkerRequest>
{
    public GetBusinessCentralMergeMarkerRequestValidator()
    {
        RuleFor(r => r.EntityType).NotEmpty();
    }
}