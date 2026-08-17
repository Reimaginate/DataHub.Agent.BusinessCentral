using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

internal static class IntegrationConfiguration
{
    public static IConfigurationRoot Build(Type? userSecretsMarkerType = null)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        try
        {
            if (userSecretsMarkerType is not null)
            {
                builder.AddUserSecrets(userSecretsMarkerType.Assembly, optional: true);
            }
            else
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                             .Where(assembly => assembly.GetCustomAttribute<UserSecretsIdAttribute>() is not null)
                             .Distinct())
                {
                    builder.AddUserSecrets(assembly, optional: true);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Environment variables remain available in restricted test runners.
        }
        catch (IOException)
        {
            // Environment variables remain available in restricted test runners.
        }

        return builder.Build();
    }
}
