namespace App.Parsers.Sources;

public sealed class LinkedInJobParser : DelimitedJobEmailParser
{
    public override string Key => "linkedin";
    protected override string SenderMarker => "linkedin.com";
}
