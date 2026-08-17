page 71010 "Data Hub Sales Doc Res."
{
    PageType = API;
    APIPublisher = 'reimaginate';
    APIGroup = 'dataHub';
    APIVersion = 'v1.0';
    EntityName = 'salesDocumentReservation';
    EntitySetName = 'salesDocumentReservations';
    SourceTable = "Sales Header";
    ODataKeyFields = SystemId;
    DelayedInsert = true;
    InsertAllowed = true;
    ModifyAllowed = false;
    DeleteAllowed = false;

    layout
    {
        area(Content)
        {
            repeater(Reservations)
            {
                field(id; Rec.SystemId)
                {
                    Caption = 'Id';
                    Editable = false;
                }
                field(correlationId; Rec."Data Hub Correlation Id")
                {
                    Caption = 'Correlation Id';
                }
                field(documentType; Rec."Document Type")
                {
                    Caption = 'Document Type';
                }
                field(customerId; CustomerId)
                {
                    Caption = 'Customer Id';
                }
            }
        }
    }

    trigger OnAfterGetRecord()
    var
        Customer: Record Customer;
    begin
        Clear(CustomerId);
        if Customer.Get(Rec."Sell-to Customer No.") then
            CustomerId := Customer.SystemId;
    end;

    trigger OnInsertRecord(BelowxRec: Boolean): Boolean
    var
        Customer: Record Customer;
        CorrelationId: Guid;
        RequestedDocumentType: Enum "Sales Document Type";
    begin
        CorrelationId := Rec."Data Hub Correlation Id";
        RequestedDocumentType := Rec."Document Type";

        if IsNullGuid(CorrelationId) then
            Error(CorrelationRequiredErr);
        if RequestedDocumentType <> RequestedDocumentType::Order then
            Error(SupportedTypeErr, Format(RequestedDocumentType));
        if IsNullGuid(CustomerId) or not Customer.GetBySystemId(CustomerId) then
            Error(CustomerRequiredErr, CustomerId);

        Rec.Init();
        Rec.Validate("Document Type", RequestedDocumentType);
        Rec."Data Hub Correlation Id" := CorrelationId;
        Rec.Validate("Sell-to Customer No.", Customer."No.");
        Rec.Insert(true);
        exit(false);
    end;

    var
        CustomerId: Guid;
        CorrelationRequiredErr: Label 'correlationId must be a non-empty GUID.';
        CustomerRequiredErr: Label 'customerId %1 does not identify a Business Central customer.';
        SupportedTypeErr: Label 'Document type %1 is not supported. The Data Hub correlation API currently supports Order only.';
}
