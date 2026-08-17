
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;

public class BusinessCentralAgentOptions
{
    public string? AgentId { get; set; }
    public string? DataSource { get; set; } = "BusinessCentral";
    public int? SalesOrderStartingJobNumber { get; set; }
    public string? Environment { get; set; }
    public ProcessingLockOptions ProcessingLockOptions { get; set; } = new();
}
