permissionset 71020 "DH DATAHUB CORR"
{
    Assignable = true;
    Caption = 'Data Hub correlation reservations';
    Permissions =
        tabledata Customer = R,
        tabledata Vendor = R,
        tabledata Item = R,
        tabledata "Sales Header" = RIM,
        tabledata "Sales Line" = RIM,
        tabledata "Purchase Header" = RIM,
        tabledata "Purchase Line" = RIM,
        page "Data Hub Sales Doc Res." = X,
        page "Data Hub Sales Line Res." = X,
        page "Data Hub Purchase Doc Res." = X,
        page "Data Hub Purchase Line Res." = X,
        page "Data Hub G/L Entries" = X,
        codeunit "Data Hub Correlation Guard" = X;
}
