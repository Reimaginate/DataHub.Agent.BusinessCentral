namespace Reimaginate.DataHub.Agent.BusinessCentral.Contracts;

public class BusinessCentralBatchRequestResponse<T>
{
    public string id { get; set; }
    public int status { get; set; }
    public T body { get; set; }
}

public class BusinessCentralBatchRequestErrorResponse
{
    public BusinessCentralBatchRequestError Error { get; set; }
}

public class BusinessCentralBatchRequestError
{
    public string Code { get; set; }
    public string Message { get; set; }
}

//"error": {
//    "code": "Application_DialogException",
//    "message": "Line Type must be Item  CorrelationId:  31593e31-5570-4dfb-ad99-9fda1335c204."
//}