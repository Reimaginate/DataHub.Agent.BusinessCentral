using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Commands;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Keep the precedence visible for users copying the starter:
// example defaults < local untracked JSON < user-secrets < environment variables.
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.example.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Services.AddBusinessCentralReference(builder.Configuration);
builder.Services.AddScoped<CommandRunner>();

var errors = StarterConfiguration.Validate(builder.Configuration);
if (errors.Count > 0)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine(error);
    }
    return 2;
}

using var host = builder.Build();
ReferenceRegistration.ValidateRegistrations(host.Services);

if (args.Length == 1 && args[0].Equals("--worker", StringComparison.OrdinalIgnoreCase))
{
    await host.RunAsync();
    return 0;
}

try
{
    await using var scope = host.Services.CreateAsyncScope();
    return await scope.ServiceProvider.GetRequiredService<CommandRunner>().RunAsync(args);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

public partial class Program;
