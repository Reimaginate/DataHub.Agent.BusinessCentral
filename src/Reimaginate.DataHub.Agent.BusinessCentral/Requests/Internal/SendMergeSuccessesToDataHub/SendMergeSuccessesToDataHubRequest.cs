using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeSuccessesToDataHub
{
    public class SendMergeSuccessesToDataHubRequest : IRequest<NullResponse>
    {
        public SendMergeSuccessesToDataHubRequest()
        {

        }

        public SendMergeSuccessesToDataHubRequest(List<MergeEntityResult> successes)
        {
            Successes = successes;
        }

        public List<MergeEntityResult> Successes { get; set; }
    }
}
