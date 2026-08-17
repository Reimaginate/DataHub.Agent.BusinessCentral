tableextension 71001 "Data Hub Sales Line" extends "Sales Line"
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
