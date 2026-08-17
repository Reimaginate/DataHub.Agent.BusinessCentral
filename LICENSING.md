# Licensing

## Agent packages and correlation extension

Reimaginate Pty Ltd licenses all Reimaginate-authored source in this repository
and the corresponding official Agent package contents under the
[MIT License](LICENSE). This includes the optional Reimaginate Data Hub
Correlation AL extension source.

For clarity, this is an express separate MIT licence grant for the copies
distributed from this Agent repository and its official packages, including
portions that may also appear in, or were adapted from, Reimaginate DataHub. It
does not alter the licence that applies to copies distributed as part of
DataHub.

Microsoft Business Central symbols and compiled application packages are not
distributed from this repository. Microsoft Dynamics 365 Business Central and
its standard APIs remain subject to Microsoft's applicable licence terms.

## DataHub dependencies

The Business Central runtime communicates with DataHub through separately
distributed DataHub client and shared-contract packages. The Business Central
test-framework package additionally provides optional in-process DataHub test
hosting and therefore depends on the DataHub runtime and test framework.

Those dependencies retain the
[DataHub Business Source License 1.1](https://github.com/Reimaginate/DataHub/blob/v1.4.0/LICENSE).
The Agent's MIT licence does not relicense them. Full DataHub runtime APIs are
used only by test-framework integration support.

The exact restored dependency inventory and applicable licences are recorded in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
