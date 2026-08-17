tableextension 71003 "Data Hub Purchase Line" extends "Purchase Line"
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
