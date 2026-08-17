# Reimaginate Data Hub correlation extension

This optional per-tenant extension gives the Data Hub Business Central agent a
server-enforced correlation key for sales-order, sales-order-line,
purchase-order, and purchase-order-line creates. Version 1.0.1.0 also exposes a
read-only G/L-entry endpoint for the dedicated integration-test application.

Business Central does not allow Microsoft API pages to be extended, so the app
adds a small reservation API instead of replacing or modifying the standard
v2.0 API. The agent creates a minimal order or item line through the reservation
API, reads the same system ID through the standard API, then applies the mapped
fields with an exact-ETag PATCH. A retry uses the same deterministic correlation
ID and recovers the existing reservation rather than posting another document.

## API surface

Publisher/group/version: `reimaginate/dataHub/v1.0`

- `salesDocumentReservations`
- `salesDocumentLineReservations`
- `purchaseDocumentReservations`
- `purchaseDocumentLineReservations`
- `generalLedgerEntries` (read-only)

The four endpoints accept `correlationId` and `documentType`. Header endpoints
also require `customerId` or `vendorId`; line endpoints require `documentId` and
`itemId`. Version 1 supports the `Order` document type only. The reservation
pages permit insert and read, but not modify or delete.

The correlation field is immutable and uniqueness is checked under a table lock
on each supported source table. The API returns the standard record `SystemId`,
which is also the ID used by Microsoft's standard v2 API.

The standard APIV2 application-scope entitlement cannot entitle an Entra
application to Base Application Table Data 17. The custom `generalLedgerEntries`
page therefore carries the narrowly scoped inherent read permission for G/L
Entry. Per-tenant extensions cannot define Entitlement objects; access to the
page is instead controlled by assigning the extension's `DH DATAHUB CORR`
permission set to the integration application for the isolated company.

## Build and install

1. Open this folder in Visual Studio Code with Microsoft's AL Language
   extension installed.
2. Set the target sandbox in `.vscode/launch.json` without committing tenant
   credentials.
3. Run **AL: Download Symbols**, then **AL: Package**. The generated `.app` is
   ignored by Git.
4. In the isolated Business Central environment, open **Extension Management**,
   choose **Manage > Upload Extension**, select the `.app`, accept the
   disclaimer, and deploy it for the current version.
5. On **Microsoft Entra applications**, open the Data Hub test application and
   assign `DH DATAHUB CORR` for the isolated test company only. Retain the
   standard `D365 SALES DOC, EDIT` and `D365 PURCH DOC, EDIT` permission sets.
   Do not assign posting permission sets or `SUPER`.

The included permission set grants read access to Customer, Vendor, and Item,
read/insert/modify access to Sales Header/Line and Purchase Header/Line, and
execute access to the read-only G/L-entry API page. The G/L page itself grants
only the indirect read needed to project Table Data 17. The extension does not
grant delete or posting rights.

## Agent configuration

Keep reservations disabled until the exact extension version is installed and
the custom endpoint has been validated:

```json
{
  "BusinessCentralAgentOptions": {
    "BusinessCentralServiceOptions": {
      "CorrelationReservationsEnabled": true,
      "CorrelationApiRoute": "api/reimaginate/dataHub/v1.0"
    }
  }
}
```

For the integration harness, use user-secrets:

```powershell
$project = "test/Reimaginate.DataHub.Agent.BusinessCentral.Tests/IntegrationTests/Reimaginate.DataHub.Agent.BusinessCentral.Tests.Integration.csproj"
dotnet user-secrets --project $project set "BusinessCentralIntegrationTests:CorrelationReservationsEnabled" "true"
dotnet user-secrets --project $project set "BusinessCentralIntegrationTests:CorrelationApiRoute" "api/reimaginate/dataHub/v1.0"
```

If the extension is absent, disabled, or unauthorized, turn the option off. The
agent then retains its existing fail-closed standard-API behavior for ambiguous
creates; it must not silently fall back from a failed reservation to an
un-correlated POST.

## Safety boundary

- No endpoint posts, releases, ships, receives, invoices, sends, cancels, or
  deletes a document.
- The G/L-entry endpoint disables insert, modify, and delete and exposes only the
  immutable fields required by the Data Hub accounting snapshot.
- Only a customer/vendor and item already readable by the caller may be used.
- Correlation IDs cannot be changed or cleared after insert.
- The app currently reserves only orders and item lines. Other document types
  continue to use their existing standard-API create behavior.

Microsoft documents that API pages cannot be extended and that custom read/write
APIs must be new API page objects:
[API development overview](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-api),
[API page type](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-api-pagetype).
For deployment, see
[Install and uninstall extensions](https://learn.microsoft.com/en-gb/dynamics365/business-central/ui-extensions-install-uninstall).
