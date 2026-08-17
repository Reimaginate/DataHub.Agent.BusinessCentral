# Reimaginate.DataHub.Agent.BusinessCentral.Abstractions

Lightweight contracts used by the Business Central agent and by implementation-owned Business Central models and mappings.

Concrete Business Central API models and Data Hub models belong to the consuming implementation or its test suite, not this package.

Use the attributes in this package to declare standard/custom entity routes,
parent-scoped routes, incremental timestamps, Business Central date fields,
create-recovery keys and optional correlation-reservation fields. Implement
`IDataHubTypeMapper<TDataHub,TBusinessCentral>` on outbound mappings so the agent
can resolve dependent entity references before synchronization.

Source: https://github.com/Reimaginate/DataHub.Agent.BusinessCentral
Support: support@reimaginate.online
