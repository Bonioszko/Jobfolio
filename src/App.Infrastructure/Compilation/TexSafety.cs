using System.Text;
using System.Text.RegularExpressions;

namespace App.Infrastructure;

public static partial class TexSafety
{
    public static void Validate(string tex, int maxUtf8Bytes = 200_000)
    {
        ArgumentNullException.ThrowIfNull(tex);

        if (Encoding.UTF8.GetByteCount(tex) > maxUtf8Bytes)
        {
            throw new InvalidOperationException($"TeX input exceeds {maxUtf8Bytes} bytes.");
        }

        var texWithoutAllowedInputs = AllowedInputPattern().Replace(tex, string.Empty);
        if (UnsafePattern().IsMatch(texWithoutAllowedInputs))
        {
            throw new InvalidOperationException("Unsafe TeX command or path detected.");
        }
    }

    [GeneratedRegex(@"\\input\s*\{\s*glyphtounicode\s*\}")]
    private static partial Regex AllowedInputPattern();

    [GeneratedRegex(
        @"\\(write18|immediate|openout|read|input|include)\b|\.\.[\\/]|(?:^|\s)[A-Za-z]:[\\/]",
        RegexOptions.IgnoreCase)]
    private static partial Regex UnsafePattern();
}

public sealed class TexSafetyValidator(int maxUtf8Bytes) : App.Application.ITexSafetyValidator
{
    public void Validate(string tex) => TexSafety.Validate(tex, maxUtf8Bytes);
}
