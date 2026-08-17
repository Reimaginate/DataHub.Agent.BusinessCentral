using FluentValidation;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RegisterAlternateKey;

public class RegisterAlternateKeyRequestValidator: AbstractValidator<RegisterAlternateKeyRequest>
{
    public RegisterAlternateKeyRequestValidator()
    {
        RuleFor(r => r.EntityType).NotNull();
        RuleFor(r => r.DataHubEntityId).NotNull();
        RuleFor(r => r.SourceSystemEntityId).NotNull();
    }
}