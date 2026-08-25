# DataHub Agent for Business Central

This repository contains the buildable public source snapshot for version
1.0.0-beta.3 of the Reimaginate DataHub Business Central packages:

- `Reimaginate.DataHub.Agent.BusinessCentral.Abstractions`
- `Reimaginate.DataHub.Agent.BusinessCentral`
- `Reimaginate.DataHub.Agent.TestFramework.BusinessCentral`

The runtime synchronizes implementation-owned Data Hub models with Microsoft
Dynamics 365 Business Central API records. The Abstractions package contains
the routing, document and mapping contracts used by those models. The test
framework provides Scenario Builder helpers and deterministic integration
support.

## Install

```powershell
dotnet add package Reimaginate.DataHub.Agent.BusinessCentral --version 1.0.0-beta.3
dotnet add package Reimaginate.DataHub.Agent.TestFramework.BusinessCentral --version 1.0.0-beta.3
```

Mapping-only projects may reference
`Reimaginate.DataHub.Agent.BusinessCentral.Abstractions` directly. The runtime
package already brings it in transitively.

## Copyable starter implementation

The `samples` directory contains a runnable starter that can be copied into a
consumer solution and connected to an existing DataHub. It includes:

- Data Hub Account and Business Central Customer;
- Data Hub Product and Business Central Item; and
- Data Hub Sales Order and Sales Order Line.

It demonstrates complete DataHub-client, mapper, mediator, processing-lock and
closed entity-pair registration; `DefaultAzureCredential`; standard
`api/v2.0` routing; optional correlation reservations; safe specific and
incremental merge/sync commands; dependency-ordered background processing; and
deterministic tests that need no external credentials.

Copy `appsettings.example.json` to a local untracked settings file. Supply Azure
identity values through the standard Azure Identity environment variables,
managed identity, workload identity, or your developer login. Never commit a
client secret. Run `--validate`, then the read-only `--smoke` command, before
enabling writes. The sample README contains complete DataHub shared-key,
application-registration and managed-identity examples plus a step-by-step
copy/customise/deploy guide.

## Optional correlation extension

`businesscentral/Reimaginate.DataHub.Correlation` contains source for the
optional per-tenant AL extension. It supplies server-enforced create
correlation for sales and purchase orders and a narrowly scoped read-only G/L
entry endpoint. Its README documents compilation, deployment and the required
company-scoped permission set. No compiled `.app` or downloaded Microsoft
symbol package is distributed from this repository.

## Build and test

Install the .NET 10 SDK, then run:

```powershell
dotnet restore .\Reimaginate.DataHub.Agent.BusinessCentral.slnx
dotnet build .\Reimaginate.DataHub.Agent.BusinessCentral.slnx --configuration Release
dotnet test .\Reimaginate.DataHub.Agent.BusinessCentral.slnx --configuration Release
```

The live sandbox integration suite is maintained privately because it contains
tenant-specific provisioning and safety controls. Public tests use deterministic
HTTP transports and require no credentials.

## Beta status

This is a beta release. Public APIs, supported entity boundaries and setup
requirements may change before version 1.0. Financial-document examples are
draft-only unless explicitly documented otherwise; the agent never implicitly
posts, releases, ships, receives or invoices a document.

## Vendor-maintained source

This project is maintained and released by Reimaginate. Its source is provided
under the MIT License for transparency, debugging, customisation and customer
assurance. Reimaginate supports only official, unmodified builds unless agreed
under a commercial support arrangement.

External pull requests are not accepted. Reproducible bugs and feature requests
are welcome through the structured issue forms, but GitHub Issues are not a
support channel. Report security vulnerabilities privately as described in
[SECURITY.md](SECURITY.md).

## License

Reimaginate-authored Agent and AL-extension source is licensed under the
[MIT License](LICENSE). DataHub dependencies retain their separate Business
Source License 1.1. See [LICENSING.md](LICENSING.md) and
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
