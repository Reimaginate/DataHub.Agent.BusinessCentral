using FluentValidation;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralMergeMarker;

public class UpdateBusinessCentralMergeMarkerRequestValidator : AbstractValidator<UpdateBusinessCentralMergeMarkerRequest>
{
    public UpdateBusinessCentralMergeMarkerRequestValidator()
    {
        RuleFor(r => r.Marker).NotNull();
        RuleFor(r => r.Marker!.AgentId).NotEmpty();
        RuleFor(r => r.Marker!.DataSource).NotEmpty();
        RuleFor(r => r.Marker!.EntityType).NotEmpty();
        RuleFor(r => r.NewValue).NotEmpty();
    }
}