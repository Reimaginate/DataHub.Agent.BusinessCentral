namespace Reimaginate.DataHub.Agent.BusinessCentral.Contracts;

public class BusinessCentralApiBatchRequest
{
    public string method { get; set; }
    public string id { get; set; }
    public string url { get; set; }
    public Dictionary<string, string> headers { get; set; }
    public object body { get; set; }
}