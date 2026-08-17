using FluentValidation;
// ReSharper disable InconsistentNaming


namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralSyncMarker;

public class UpdateBusinessCentralSyncMarkerRequestValidator : AbstractValidator<UpdateBusinessCentralSyncMarkerRequest>
{
    public UpdateBusinessCentralSyncMarkerRequestValidator()
    {
        RuleFor(r => r.Marker).NotNull();
        RuleFor(r => r.Marker!.AgentId).NotEmpty();
        RuleFor(r => r.Marker!.DataSource).NotEmpty();
        RuleFor(r => r.Marker!.EntityType).NotEmpty();
        RuleFor(r => r.NewValue).NotEmpty();
    }
}