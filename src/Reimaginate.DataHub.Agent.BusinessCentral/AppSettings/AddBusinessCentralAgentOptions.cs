using Microsoft.Extensions.Configuration;

namespace Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;

public class AddBusinessCentralAgentOptions
{
    internal IConfiguration Config { get; set; } = new ConfigurationBuilder().Build();
    internal BusinessCentralAgentOptions BusinessCentralAgentOptions { get; set; } = new();
    internal BusinessCentralServiceOptions BusinessCentralServiceOptions { get; set; } = new();

    public AddBusinessCentralAgentOptions WithAgentId(string agentId)
    {
        BusinessCentralAgentOptions.AgentId = agentId;
        return this;
    }

    public AddBusinessCentralAgentOptions WithDataSourceId(string dataSourceId)
    {
        BusinessCentralAgentOptions.DataSource = dataSourceId;
        return this;
    }

    public AddBusinessCentralAgentOptions WithDataSource(string dataSource)
    {
        BusinessCentralAgentOptions.DataSource = dataSource;
        return this;
    }

    public AddBusinessCentralAgentOptions WithServiceBaseUrl(string baseUrl)
    {
        BusinessCentralServiceOptions.BaseUrl = baseUrl;
        return this;
    }

    public AddBusinessCentralAgentOptions WithAppSettingsConfig(IConfiguration config, string key = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        if (!string.IsNullOrEmpty(key))
        {
            Config = Config.GetSection(key);
        }

        Config.Bind(BusinessCentralAgentOptions);
        Config.GetSection("BusinessCentralServiceOptions").Bind(BusinessCentralServiceOptions);
        return this;
    }
}
