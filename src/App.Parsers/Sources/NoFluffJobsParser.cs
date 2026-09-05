namespace App.Parsers.Sources;

public sealed class NoFluffJobsParser : DelimitedJobEmailParser
{
    public override string Key => "nofluffjobs";
    protected override string SenderMarker => "nofluffjobs.com";
}
