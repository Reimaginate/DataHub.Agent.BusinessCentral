using Microsoft.Extensions.DependencyInjection;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Services;

public static class ServicesBuilder
{
    public static void Build(IServiceCollection services)
    {
        services.AddBusinessCentralAgent();
    }
}
