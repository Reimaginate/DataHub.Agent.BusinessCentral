using FluentValidation;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;

public class GetBusinessCentralSyncMarkerRequestValidator : AbstractValidator<GetBusinessCentralSyncMarkerRequest>
{
    public GetBusinessCentralSyncMarkerRequestValidator()
    {
        RuleFor(r => r.EntityType).NotEmpty();
    }
}