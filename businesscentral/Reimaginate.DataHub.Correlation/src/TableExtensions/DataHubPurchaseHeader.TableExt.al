tableextension 71002 "Data Hub Purchase Header" extends "Purchase Header"
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
