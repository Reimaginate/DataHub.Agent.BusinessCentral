namespace Reimaginate.DataHub.Correlation;

using Microsoft.Finance.GeneralLedger.Ledger;

page 71030 "Data Hub G/L Entries"
{
    APIGroup = 'dataHub';
    APIPublisher = 'reimaginate';
    APIVersion = 'v1.0';
    Caption = 'Data Hub G/L Entries';
    DelayedInsert = false;
    DeleteAllowed = false;
    Editable = false;
    EntityName = 'generalLedgerEntry';
    EntitySetName = 'generalLedgerEntries';
    Extensible = false;
    InsertAllowed = false;
    ModifyAllowed = false;
    ODataKeyFields = SystemId;
    PageType = API;
    Permissions = tabledata "G/L Entry" = R;
    SourceTable = "G/L Entry";

    layout
    {
        area(Content)
        {
            repeater(Entries)
            {
                field(id; Rec.SystemId)
                {
                    Caption = 'Id';
                }
                field(entryNumber; Rec."Entry No.")
                {
                    Caption = 'Entry Number';
                }
                field(postingDate; Rec."Posting Date")
                {
                    Caption = 'Posting Date';
                }
                field(documentNumber; Rec."Document No.")
                {
                    Caption = 'Document Number';
                }
                field(documentType; Rec."Document Type")
                {
                    Caption = 'Document Type';
                }
                field(accountId; Rec."Account Id")
                {
                    Caption = 'Account Id';
                }
                field(accountNumber; Rec."G/L Account No.")
                {
                    Caption = 'Account Number';
                }
                field(description; Rec.Description)
                {
                    Caption = 'Description';
                }
                field(debitAmount; Rec."Debit Amount")
                {
                    Caption = 'Debit Amount';
                }
                field(creditAmount; Rec."Credit Amount")
                {
                    Caption = 'Credit Amount';
                }
                field(additionalCurrencyDebitAmount; Rec."Add.-Currency Debit Amount")
                {
                    Caption = 'Additional Currency Debit Amount';
                }
                field(additionalCurrencyCreditAmount; Rec."Add.-Currency Credit Amount")
                {
                    Caption = 'Additional Currency Credit Amount';
                }
                field(lastModifiedDateTime; Rec.SystemModifiedAt)
                {
                    Caption = 'Last Modified Date Time';
                }
            }
        }
    }
}
