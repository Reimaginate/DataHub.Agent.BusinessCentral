codeunit 71004 "Data Hub Correlation Guard"
{
    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnBeforeInsertEvent', '', false, false)]
    local procedure SalesHeaderOnBeforeInsert(var Rec: Record "Sales Header"; RunTrigger: Boolean)
    var
        Existing: Record "Sales Header";
    begin
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnBeforeModifyEvent', '', false, false)]
    local procedure SalesHeaderOnBeforeModify(var Rec: Record "Sales Header"; var xRec: Record "Sales Header"; RunTrigger: Boolean)
    var
        Existing: Record "Sales Header";
    begin
        if Rec."Data Hub Correlation Id" <> xRec."Data Hub Correlation Id" then
            Error(CorrelationImmutableErr);
        EnsureUniqueSalesHeader(Rec, Existing);
    end;

    [EventSubscriber(ObjectType::Table, Database::"Sales Line", 'OnBeforeInsertEvent', '', false, false)]
    local procedure SalesLineOnBeforeInsert(var Rec: Record "Sales Line"; RunTrigger: Boolean)
    var
        Existing: Record "Sales Line";
    begin
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    [EventSubscriber(ObjectType::Table, Database::"Sales Line", 'OnBeforeModifyEvent', '', false, false)]
    local procedure SalesLineOnBeforeModify(var Rec: Record "Sales Line"; var xRec: Record "Sales Line"; RunTrigger: Boolean)
    var
        Existing: Record "Sales Line";
    begin
        if Rec."Data Hub Correlation Id" <> xRec."Data Hub Correlation Id" then
            Error(CorrelationImmutableErr);
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        Existing.SetFilter(SystemId, '<>%1', Rec.SystemId);
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    [EventSubscriber(ObjectType::Table, Database::"Purchase Header", 'OnBeforeInsertEvent', '', false, false)]
    local procedure PurchaseHeaderOnBeforeInsert(var Rec: Record "Purchase Header"; RunTrigger: Boolean)
    var
        Existing: Record "Purchase Header";
    begin
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    [EventSubscriber(ObjectType::Table, Database::"Purchase Header", 'OnBeforeModifyEvent', '', false, false)]
    local procedure PurchaseHeaderOnBeforeModify(var Rec: Record "Purchase Header"; var xRec: Record "Purchase Header"; RunTrigger: Boolean)
    var
        Existing: Record "Purchase Header";
    begin
        if Rec."Data Hub Correlation Id" <> xRec."Data Hub Correlation Id" then
            Error(CorrelationImmutableErr);
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        Existing.SetFilter(SystemId, '<>%1', Rec.SystemId);
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    [EventSubscriber(ObjectType::Table, Database::"Purchase Line", 'OnBeforeInsertEvent', '', false, false)]
    local procedure PurchaseLineOnBeforeInsert(var Rec: Record "Purchase Line"; RunTrigger: Boolean)
    var
        Existing: Record "Purchase Line";
    begin
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    [EventSubscriber(ObjectType::Table, Database::"Purchase Line", 'OnBeforeModifyEvent', '', false, false)]
    local procedure PurchaseLineOnBeforeModify(var Rec: Record "Purchase Line"; var xRec: Record "Purchase Line"; RunTrigger: Boolean)
    var
        Existing: Record "Purchase Line";
    begin
        if Rec."Data Hub Correlation Id" <> xRec."Data Hub Correlation Id" then
            Error(CorrelationImmutableErr);
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        Existing.SetFilter(SystemId, '<>%1', Rec.SystemId);
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    local procedure EnsureUniqueSalesHeader(Rec: Record "Sales Header"; var Existing: Record "Sales Header")
    begin
        if IsNullGuid(Rec."Data Hub Correlation Id") then
            exit;

        Existing.LockTable();
        Existing.SetRange("Data Hub Correlation Id", Rec."Data Hub Correlation Id");
        Existing.SetFilter(SystemId, '<>%1', Rec.SystemId);
        if not Existing.IsEmpty() then
            Error(CorrelationAlreadyExistsErr, Rec."Data Hub Correlation Id", Existing.TableCaption());
    end;

    var
        CorrelationAlreadyExistsErr: Label 'Data Hub correlation id %1 already identifies a %2. Retry with the original Data Hub entity instead of creating another record.';
        CorrelationImmutableErr: Label 'A Data Hub correlation id cannot be changed or cleared after the record is created.';
}
