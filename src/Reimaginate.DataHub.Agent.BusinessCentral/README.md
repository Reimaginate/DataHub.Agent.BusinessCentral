# Reimaginate.DataHub.Agent.BusinessCentral

Microsoft Dynamics 365 Business Central agent for synchronizing implementation-owned
Data Hub models through the standard Business Central API.

The package provides explicit and incremental merge/sync workflows, pagination,
exact-ETag mutation, retry and ambiguous-create safeguards, reference resolution,
processing locks, and configurable standard or custom API routes. Concrete models
and mappings are supplied by the consuming implementation through
`Reimaginate.DataHub.Agent.BusinessCentral.Abstractions`.

This beta targets .NET 10. See the public repository for the reference
implementation, authentication setup, optional correlation extension, supported
scenarios and safety boundaries.

Source: https://github.com/Reimaginate/DataHub.Agent.BusinessCentral
Support: support@reimaginate.online
