tableextension 71000 "Data Hub Sales Header" extends "Sales Header"
{
    fields
    {
        field(71000; "Data Hub Correlation Id"; Guid)
        {
            Caption = 'Data Hub Correlation Id';
            DataClassification = SystemMetadata;
        }
    }

    keys
    {
        key(DataHubCorrelation; "Data Hub Correlation Id")
        {
        }
    }
}
