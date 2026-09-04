namespace App.Parsers.Sources;
public sealed class JustJoinItJobParser : DelimitedJobEmailParser
{
    public override string Key => "justjoin.it";
    protected override string SenderMarker => "justjoin.it";
}
