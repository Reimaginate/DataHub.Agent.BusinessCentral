using FluentValidation;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeSuccessesToDataHub;

public class SendMergeSuccessesToDataHubRequestValidator : AbstractValidator<SendMergeSuccessesToDataHubRequest>
{
    public SendMergeSuccessesToDataHubRequestValidator()
    {
        RuleFor(r => r.Successes).NotEmpty();
    }

}