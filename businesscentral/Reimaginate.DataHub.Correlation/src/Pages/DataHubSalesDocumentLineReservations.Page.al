page 71011 "Data Hub Sales Line Res."
{
    PageType = API;
    APIPublisher = 'reimaginate';
    APIGroup = 'dataHub';
    APIVersion = 'v1.0';
    EntityName = 'salesDocumentLineReservation';
    EntitySetName = 'salesDocumentLineReservations';
    SourceTable = "Sales Line";
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
                field(documentId; DocumentId)
                {
                    Caption = 'Document Id';
                }
                field(itemId; ItemId)
                {
                    Caption = 'Item Id';
                }
            }
        }
    }

    trigger OnAfterGetRecord()
    var
        Header: Record "Sales Header";
        Item: Record Item;
    begin
        Clear(DocumentId);
        Clear(ItemId);
        if Header.Get(Rec."Document Type", Rec."Document No.") then
            DocumentId := Header.SystemId;
        if (Rec.Type = Rec.Type::Item) and Item.Get(Rec."No.") then
            ItemId := Item.SystemId;
    end;

    trigger OnInsertRecord(BelowxRec: Boolean): Boolean
    var
        Header: Record "Sales Header";
        Item: Record Item;
        ExistingLine: Record "Sales Line";
        CorrelationId: Guid;
        NextLineNumber: Integer;
    begin
        CorrelationId := Rec."Data Hub Correlation Id";
        if IsNullGuid(CorrelationId) then
            Error(CorrelationRequiredErr);
        if IsNullGuid(DocumentId) or not Header.GetBySystemId(DocumentId) then
            Error(DocumentRequiredErr, DocumentId);
        if Header."Document Type" <> Header."Document Type"::Order then
            Error(SupportedTypeErr, Format(Header."Document Type"));
        if IsNullGuid(ItemId) or not Item.GetBySystemId(ItemId) then
            Error(ItemRequiredErr, ItemId);

        ExistingLine.SetRange("Document Type", Header."Document Type");
        ExistingLine.SetRange("Document No.", Header."No.");
        if ExistingLine.FindLast() then
            NextLineNumber := ExistingLine."Line No." + 10000
        else
            NextLineNumber := 10000;

        Rec.Init();
        Rec.Validate("Document Type", Header."Document Type");
        Rec.Validate("Document No.", Header."No.");
        Rec."Line No." := NextLineNumber;
        Rec."Data Hub Correlation Id" := CorrelationId;
        Rec.Validate(Type, Rec.Type::Item);
        Rec.Validate("No.", Item."No.");
        Rec.Insert(true);
        exit(false);
    end;

    var
        DocumentId: Guid;
        ItemId: Guid;
        CorrelationRequiredErr: Label 'correlationId must be a non-empty GUID.';
        DocumentRequiredErr: Label 'documentId %1 does not identify a Business Central sales order.';
        ItemRequiredErr: Label 'itemId %1 does not identify a Business Central item.';
        SupportedTypeErr: Label 'Document type %1 is not supported. The Data Hub correlation API currently supports Order only.';
}
