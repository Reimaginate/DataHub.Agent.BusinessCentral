# Copyable Business Central agent starter

This is a runnable starting point for connecting a Business Central agent to an
existing Reimaginate DataHub. Copy this project and, optionally, its test
project into your solution. The infrastructure is complete; the intended
customisation points are the DataHub models, Business Central models, mappings,
entity-pair registrations and processing order.

The starter includes Account/Customer, Product/Item and Sales Order/Line. It
uses the standard Business Central `api/v2.0` route and does not call document
posting, release, shipment or invoicing actions.

## 1. Copy the projects

Copy:

```text
samples/Reimaginate.DataHub.Agent.BusinessCentral.Reference
samples/Reimaginate.DataHub.Agent.BusinessCentral.Reference.Tests
```

Rename the projects and namespaces if desired. The executable project consumes
the published `Reimaginate.DataHub.Agent.BusinessCentral` package rather than a
private repository project.

Search for `START HERE`. Those comments identify the normal customisation
points. Do not copy private integration-test configuration or credentials.

## 2. Prepare the existing DataHub

Confirm that the DataHub exposes the client API and contains the canonical
entity types you intend to use. The sample expects these names:

| DataHub entity | Business Central source type | Standard route |
| --- | --- | --- |
| `Account` | `Customer` | `customers` |
| `Product` | `Item` | `items` |
| `SalesOrder` | `SalesOrder` | `salesOrders` |
| `SalesOrderLine` | `SalesOrderLine` | `salesOrderLines` |

`RelatedEntityType` on each DataHub model establishes the related source type.
The agent stores Business Central GUIDs as DataHub alternate keys such as
`BusinessCentral.Customer`. Those keys make later updates target the same
record and resolve parent/customer/product references.

Use an Agent ID unique to this deployed process. `DataSource` must match the
Business Central data-source name registered in your DataHub.

## 3. Prepare Business Central security

Create or reuse a single-tenant Entra application with Business Central
application permission `API.ReadWrite.All` and admin consent. Add that client ID
to the target company's **Microsoft Entra applications** page, enable it and
assign only the company-scoped permission sets required for the entities in
your mappings. Do not assign `SUPER`.

The starter authenticates Business Central with `DefaultAzureCredential` and
scope `https://api.businesscentral.dynamics.com/.default`. For a deployed app,
prefer managed identity or workload identity. For a client-secret application,
use the standard Azure Identity environment variables:

```text
AZURE_TENANT_ID
AZURE_CLIENT_ID
AZURE_CLIENT_SECRET
```

Never place the secret in an appsettings file.

## 4. Configure both connections

Copy `appsettings.example.json` to `appsettings.json`. The copied file is ignored
by the starter's `.gitignore`.

Set the Business Central API environment URL and company GUID:

```json
"BusinessCentralServiceOptions": {
  "BaseUrl": "https://api.businesscentral.dynamics.com/v2.0/TENANT/ENVIRONMENT/",
  "CompanyId": "COMPANY-GUID",
  "CompanyName": "My Test Company",
  "ApiRoute": "api/v2.0"
}
```

The company GUID is the `id` returned by the authenticated standard
`companies` endpoint; it is not the display name from the browser URL.

### DataHub managed identity

```json
"DataHubClientOptions": {
  "DataHubClientUrl": "https://my-datahub.example/api/Client",
  "AuthenticationMode": "ManagedIdentity",
  "AzureAdScope": "api://DATAHUB-API-CLIENT-ID/.default",
  "ManagedIdentityClientId": "OPTIONAL-USER-ASSIGNED-IDENTITY-CLIENT-ID"
}
```

Leave `ManagedIdentityClientId` empty for a system-assigned identity.

### DataHub application registration

```json
"DataHubClientOptions": {
  "DataHubClientUrl": "https://my-datahub.example/api/Client",
  "AuthenticationMode": "ApplicationRegistration",
  "AzureAdScope": "api://DATAHUB-API-CLIENT-ID/.default",
  "TenantId": "TENANT-ID",
  "ClientId": "CALLER-APPLICATION-CLIENT-ID",
  "ClientSecret": "DO-NOT-COMMIT"
}
```

Store the secret locally with user-secrets:

```powershell
dotnet user-secrets init --project .
dotnet user-secrets --project . set "DataHubClientOptions:ClientSecret" "<secret>"
```

### DataHub shared key

Use this only where the existing DataHub still uses shared-key authentication:

```json
"DataHubClientOptions": {
  "DataHubClientUrl": "https://my-datahub.example/api/Client",
  "AuthenticationMode": "SharedKey",
  "Key": "DO-NOT-COMMIT"
}
```

Store the key through user-secrets or your deployment secret store.

Configuration precedence is:

```text
appsettings.example.json < appsettings.json < user-secrets < environment variables
```

Environment variables use double underscores, for example
`DataHubClientOptions__DataHubClientUrl`.

## 5. Validate before making network calls

```powershell
dotnet run -- --validate
```

Validation checks both connection configurations, safety settings, the DataHub
client, Business Central client, mapper, mediator and all closed entity-pair
handler registrations. It does not acquire a token or perform a write.

## 6. Run the read-only smoke test

```powershell
dotnet run -- --smoke
```

The smoke command performs:

- a one-record read from Business Central `customers`; and
- a future-dated, one-record Account query through the existing DataHub client.

Both calls are read-only. Fix authentication, URL, company or permission errors
before enabling writes.

## 7. Prove one record in each direction

Writes are disabled by default. Enable them only in an isolated test company:

```powershell
dotnet user-secrets init --project . # run once if not already initialised
dotnet user-secrets --project . set "Starter:WritesEnabled" "true"
```

Merge an existing Business Central customer into DataHub:

```powershell
dotnet run -- --merge Customer BUSINESS-CENTRAL-CUSTOMER-GUID
```

Sync an existing DataHub account to Business Central:

```powershell
dotnet run -- --sync Account DATAHUB-ACCOUNT-ID
```

The commands also support `Product`/`Item`, `SalesOrder`, and
`SalesOrderLine`. A Business Central ID is a GUID; a DataHub ID is the canonical
DataHub entity ID.

Production writes require both `WritesEnabled=true` and the separate
`AllowProductionWrites=true` acknowledgement.

## 8. Run one incremental pass or the worker

```powershell
dotnet run -- --run-once
```

The processing plan merges incremental Customers, Items and Sales Orders first,
then synchronizes Accounts, Products, Sales Orders and Sales Order Lines. This
ensures referenced records and parents have alternate keys before children run.

Standard `salesOrderLines` do not expose `lastModifiedDateTime`, so the starter
does not claim scheduled inbound line discovery. Merge a known line ID, consume
an external event, or expose a tenant extension marker if that is required.

For continuous polling, also set:

```json
"Starter": {
  "WritesEnabled": true,
  "ScheduledProcessingEnabled": true,
  "PollingIntervalSeconds": 60,
  "BatchSize": 100
}
```

Then run:

```powershell
dotnet run -- --worker
```

The worker uses DataHub merge/sync markers, per-record processing locks,
batching and safe marker advancement. A failed pass is logged and retried on the
next interval.

For multiple replicas, replace the starter's in-memory processing lock with the
Redis configuration used by your deployment.

The included Dockerfile publishes the worker without copying local settings:

```powershell
docker build -t my-business-central-agent .
docker run --rm my-business-central-agent
```

Supply configuration and identity through the hosting platform's environment,
managed identity and secret store. The container starts with `--worker`.

## 9. Replace the example contracts and mappings

The DataHub models are deliberately small. Replace them with the canonical
contracts from your solution while retaining `DataHubEntity`, `entityType` and
the appropriate `RelatedEntityType` declaration.

Business Central models describe API JSON and routing. `BusinessCentralUrl`
selects the entity set; `BusinessCentralLastModified` enables incremental
inbound processing; `BusinessCentralDate` normalises undefined Business Central
dates.

Mappings are the ownership boundary:

- outbound maps should send fields owned by DataHub;
- inbound maps should retain Business Central numbers, states and calculated
  totals; and
- parent/reference mappings must resolve the tracked Business Central GUID.

After adding a pair, add its mappers, call either
`AddBusinessCentralEntityPair` or `AddIncrementalBusinessCentralEntityPair` in
`ReferenceRegistration`, and place it in the correct dependency order in
`EntityProcessingPlan`.

## 10. Optional correlation extension

The standard Business Central API does not provide server-enforced idempotency
keys for every document create. The optional Reimaginate Data Hub Correlation
AL extension adds reservation routes for supported documents. Enable
`CorrelationReservationsEnabled` only after installing the matching extension
and assigning its company-scoped permission set.

Without a safe create-recovery key, an ambiguous create fails closed and must be
reconciled rather than blindly retried.

## Test without credentials

```powershell
dotnet test ..\Reimaginate.DataHub.Agent.BusinessCentral.Reference.Tests
```

The deterministic tests cover configuration and write guards, complete DI
registration, bidirectional mapping, relationship resolution, standard routes,
429 retry and exact-ETag conflict handling. They do not require DataHub,
Business Central, Docker or credentials.

## Deployment checklist

- Keep credentials in the deployment secret store.
- Use a unique Agent ID and the exact existing DataHub data-source name.
- Use the standard `api/v2.0` route unless a model explicitly targets an
  installed extension.
- Start with `WritesEnabled=false` and run both validation and smoke commands.
- Prove one known merge and one known sync in a sandbox.
- Configure distributed locking before running multiple replicas.
- Exclude test records from Business Central workflows and posting automation.
- Monitor per-record merge/sync failures and do not bypass marker-safety errors.
