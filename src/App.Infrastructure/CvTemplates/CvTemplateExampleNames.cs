namespace App.Infrastructure;

internal static class CvTemplateExampleNames
{
    public static string Get(string filePath) =>
        Path.GetFileNameWithoutExtension(filePath) switch
        {
            "example-1" => ".NET backend",
            "example-2" => "Fullstack",
            "example-3" => "AI native",
            var name => name
        };
}
