using FluentValidation;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncFailuresToDataHub;

public class SendSyncFailuresToDataHubRequestValidator : AbstractValidator<SendSyncFailuresToDataHubRequest>
{
    public SendSyncFailuresToDataHubRequestValidator()
    {
        RuleFor(r => r.Failures).NotEmpty();
    }
}