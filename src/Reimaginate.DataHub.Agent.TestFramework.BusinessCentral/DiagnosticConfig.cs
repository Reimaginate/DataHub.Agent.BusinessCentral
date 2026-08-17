using System.Diagnostics;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral;

public static class DiagnosticConfig
{
    public static class BusinessCentralAgent
    {
        public const string ApplicationName = "BusinessCentral";
        public const string ApplicationVersion = "1.0.0";
        public static ActivitySource ActivitySource { get; } = new(ApplicationName, ApplicationVersion);
    }

    public static class DataHubAgent
    {
        public const string ApplicationName = "DataHub";
        public const string ApplicationVersion = "1.0.0";
        public static ActivitySource ActivitySource { get; } = new(ApplicationName, ApplicationVersion);
    }
}
