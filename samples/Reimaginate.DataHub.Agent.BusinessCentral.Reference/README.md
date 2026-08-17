# Business Central reference implementation

This deliberately small implementation shows how a consumer owns its Data Hub
and Business Central models and registers their mappings with the Business
Central agent. It covers Account/Customer, Product/Item and Sales Order/Line.

## Configure

Copy `appsettings.example.json` to `appsettings.json` and keep the new file
untracked. The sample uses `DefaultAzureCredential`; use your developer login,
managed identity, workload identity, or the standard Azure Identity environment
variables. Never put a client secret in either JSON file.

The standard route is `api/v2.0`. Enable the correlation route only after the
matching AL extension is installed and its company-scoped permission set has
been assigned.

## Run

Validate configuration and dependency injection without acquiring a token:

```powershell
dotnet run --project .\samples\Reimaginate.DataHub.Agent.BusinessCentral.Reference -- --validate
```

Perform an authenticated, read-only customer query:

```powershell
dotnet run --project .\samples\Reimaginate.DataHub.Agent.BusinessCentral.Reference -- --read-customers
```

The sample contains no write command. Use the deterministic tests to study
outbound mapping and request behavior before enabling a sandbox integration.
