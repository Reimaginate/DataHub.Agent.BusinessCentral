using FluentValidation;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncSuccessesToDataHub;

public class SendSyncSuccessesToDataHubRequestValidator : AbstractValidator<SendSyncSuccessesToDataHubRequest>
{
    public SendSyncSuccessesToDataHubRequestValidator()
    {
        RuleFor(r => r.Successes).NotEmpty();
    }

}