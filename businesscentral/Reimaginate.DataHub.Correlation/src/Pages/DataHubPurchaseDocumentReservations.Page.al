page 71012 "Data Hub Purchase Doc Res."
{
    PageType = API;
    APIPublisher = 'reimaginate';
    APIGroup = 'dataHub';
    APIVersion = 'v1.0';
    EntityName = 'purchaseDocumentReservation';
    EntitySetName = 'purchaseDocumentReservations';
    SourceTable = "Purchase Header";
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
                field(vendorId; VendorId)
                {
                    Caption = 'Vendor Id';
                }
            }
        }
    }

    trigger OnAfterGetRecord()
    var
        Vendor: Record Vendor;
    begin
        Clear(VendorId);
        if Vendor.Get(Rec."Buy-from Vendor No.") then
            VendorId := Vendor.SystemId;
    end;

    trigger OnInsertRecord(BelowxRec: Boolean): Boolean
    var
        Vendor: Record Vendor;
        CorrelationId: Guid;
        RequestedDocumentType: Enum "Purchase Document Type";
    begin
        CorrelationId := Rec."Data Hub Correlation Id";
        RequestedDocumentType := Rec."Document Type";

        if IsNullGuid(CorrelationId) then
            Error(CorrelationRequiredErr);
        if RequestedDocumentType <> RequestedDocumentType::Order then
            Error(SupportedTypeErr, Format(RequestedDocumentType));
        if IsNullGuid(VendorId) or not Vendor.GetBySystemId(VendorId) then
            Error(VendorRequiredErr, VendorId);

        Rec.Init();
        Rec.Validate("Document Type", RequestedDocumentType);
        Rec."Data Hub Correlation Id" := CorrelationId;
        Rec.Validate("Buy-from Vendor No.", Vendor."No.");
        Rec.Insert(true);
        exit(false);
    end;

    var
        VendorId: Guid;
        CorrelationRequiredErr: Label 'correlationId must be a non-empty GUID.';
        VendorRequiredErr: Label 'vendorId %1 does not identify a Business Central vendor.';
        SupportedTypeErr: Label 'Document type %1 is not supported. The Data Hub correlation API currently supports Order only.';
}
