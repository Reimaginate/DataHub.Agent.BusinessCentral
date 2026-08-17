namespace Reimaginate.DataHub.Agent.BusinessCentral.Contracts;

public class BusinessCentralBatchResponse<T>
{
    public List<BusinessCentralBatchRequestResponse<T>> responses { get; set; }
}