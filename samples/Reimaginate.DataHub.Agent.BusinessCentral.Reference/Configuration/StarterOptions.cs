using Microsoft.Extensions.Configuration;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Configuration;

/// <summary>
/// Safety and scheduling settings owned by this starter host. Agent and connection
/// settings remain in BusinessCentralAgentOptions and DataHubClientOptions.
/// </summary>
public sealed class StarterOptions
{
    public const string SectionName = "Starter";

    public bool WritesEnabled { get; set; }
    public bool ScheduledProcessingEnabled { get; set; }
    public bool AllowProductionWrites { get; set; }
    public int PollingIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
}

public static class StarterConfiguration
{
    private static readonly HashSet<string> DataHubAuthenticationModes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SharedKey",
            "ApplicationRegistration",
            "ManagedIdentity"
        };

    public static IReadOnlyList<string> Validate(IConfiguration configuration)
    {
        var errors = new List<string>();
        var starter = configuration.GetSection(StarterOptions.SectionName).Get<StarterOptions>() ?? new();
        var agent = configuration.GetSection("BusinessCentralAgentOptions");
        var businessCentral = agent.GetSection("BusinessCentralServiceOptions");
        var dataHub = configuration.GetSection("DataHubClientOptions");

        Require(errors, agent["AgentId"], "BusinessCentralAgentOptions:AgentId");
        Require(errors, agent["DataSource"], "BusinessCentralAgentOptions:DataSource");
        Require(errors, agent["Environment"], "BusinessCentralAgentOptions:Environment");

        var businessCentralBaseUrl = businessCentral["BaseUrl"];
        if (!IsConfiguredHttpsUrl(businessCentralBaseUrl))
        {
            errors.Add("Set BusinessCentralAgentOptions:BusinessCentralServiceOptions:BaseUrl to the HTTPS Business Central API environment URL.");
        }

        if (!Guid.TryParse(businessCentral["CompanyId"], out var companyId) || companyId == Guid.Empty)
        {
            errors.Add("Set BusinessCentralAgentOptions:BusinessCentralServiceOptions:CompanyId to the target company GUID.");
        }

        if (!string.Equals(businessCentral["ApiRoute"], "api/v2.0", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The starter expects the standard Business Central route api/v2.0.");
        }

        if (!IsConfiguredHttpsUrl(dataHub["DataHubClientUrl"]))
        {
            errors.Add("Set DataHubClientOptions:DataHubClientUrl to the existing DataHub client API URL, normally ending in /api/Client.");
        }

        var authenticationMode = dataHub["AuthenticationMode"];
        if (string.IsNullOrWhiteSpace(authenticationMode) || !DataHubAuthenticationModes.Contains(authenticationMode))
        {
            errors.Add("Set DataHubClientOptions:AuthenticationMode to SharedKey, ApplicationRegistration, or ManagedIdentity.");
        }
        else if (authenticationMode.Equals("SharedKey", StringComparison.OrdinalIgnoreCase))
        {
            Require(errors, dataHub["Key"], "DataHubClientOptions:Key");
        }
        else
        {
            Require(errors, dataHub["AzureAdScope"], "DataHubClientOptions:AzureAdScope");
            if (authenticationMode.Equals("ApplicationRegistration", StringComparison.OrdinalIgnoreCase))
            {
                Require(errors, dataHub["TenantId"], "DataHubClientOptions:TenantId");
                Require(errors, dataHub["ClientId"], "DataHubClientOptions:ClientId");
                Require(errors, dataHub["ClientSecret"], "DataHubClientOptions:ClientSecret");
            }
        }

        if (starter.PollingIntervalSeconds < 5)
        {
            errors.Add("Starter:PollingIntervalSeconds must be at least 5 seconds.");
        }
        if (starter.BatchSize is < 1 or > 1000)
        {
            errors.Add("Starter:BatchSize must be between 1 and 1000.");
        }
        if (starter.ScheduledProcessingEnabled && !starter.WritesEnabled)
        {
            errors.Add("Starter:ScheduledProcessingEnabled requires Starter:WritesEnabled=true.");
        }

        var environment = agent["Environment"];
        if (starter.WritesEnabled && IsProduction(environment) && !starter.AllowProductionWrites)
        {
            errors.Add("Writes are enabled for a Production environment. Set Starter:AllowProductionWrites=true only after an explicit production approval.");
        }

        return errors;
    }

    public static void EnsureWritesAllowed(StarterOptions options, string? environment)
    {
        if (!options.WritesEnabled)
        {
            throw new InvalidOperationException(
                "This command can write to DataHub or Business Central. Set Starter:WritesEnabled=true only after the read-only smoke test succeeds.");
        }
        if (IsProduction(environment) && !options.AllowProductionWrites)
        {
            throw new InvalidOperationException(
                "Production writes are blocked. Set Starter:AllowProductionWrites=true only after an explicit production approval.");
        }
    }

    private static void Require(List<string> errors, string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("YOUR-", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Set {key}.");
        }
    }

    private static bool IsConfiguredHttpsUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        !value.Contains("YOUR-", StringComparison.OrdinalIgnoreCase);

    private static bool IsProduction(string? environment) =>
        string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
}
