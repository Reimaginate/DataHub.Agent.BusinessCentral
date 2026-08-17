namespace Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;

public class BusinessCentralServiceOptions
{
    public const string DefaultApiRoute = "api/inviga/datahub/v2.0";
    public const string DefaultCorrelationApiRoute = "api/reimaginate/dataHub/v1.0";

    public string? BaseUrl { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyId { get; set; }
    public string ApiRoute { get; set; } = DefaultApiRoute;
    public bool CorrelationReservationsEnabled { get; set; }
    public string CorrelationApiRoute { get; set; } = DefaultCorrelationApiRoute;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBaseDelayMilliseconds { get; set; } = 250;
}
