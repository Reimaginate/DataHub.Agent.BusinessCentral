using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.example.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddBusinessCentralReference(builder.Configuration);

var errors = ReferenceConfiguration.Validate(builder.Configuration).ToArray();
if (errors.Length > 0)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine(error);
    }
    return 2;
}

if (args.Contains("--validate", StringComparer.OrdinalIgnoreCase) || args.Length == 0)
{
    await using var validationProvider = builder.Services.BuildServiceProvider();
    _ = validationProvider.GetRequiredService<IBusinessCentralODataService>();
    Console.WriteLine("Business Central reference configuration and registrations are valid.");
    return 0;
}

if (!args.Contains("--read-customers", StringComparer.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Use --validate or --read-customers. The reference host exposes no write command.");
    return 2;
}

await using var provider = builder.Services.BuildServiceProvider();
var service = provider.GetRequiredService<IBusinessCentralODataService>();
var response = await service.GetEntitiesAsync<Customer>(top: 5);
if (response.IsT1)
{
    Console.Error.WriteLine($"Business Central returned {(int)response.AsT1.StatusCode} {response.AsT1.StatusCode}.");
    return 1;
}
if (response.IsT2)
{
    Console.Error.WriteLine(response.AsT2.Message);
    return 1;
}

foreach (var customer in response.AsT0.Value)
{
    Console.WriteLine($"{customer.Id}: {customer.Number} - {customer.DisplayName}");
}
return 0;
