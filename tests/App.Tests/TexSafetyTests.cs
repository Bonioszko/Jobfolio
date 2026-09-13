using App.Infrastructure;

namespace App.Tests;

public sealed class TexSafetyTests
{
    [Theory]
    [InlineData(@"\write18{calc}")]
    [InlineData(@"\input{glyphtounicode}")]
    [InlineData(@"\input{../secret}")]
    [InlineData(@"\input{glyphtounicode-malicious}")]
    [InlineData(@"\input{glyphtounicode}\input{secret}")]
    [InlineData(@"C:\private\file")]
    public void Unsafe_tex_is_rejected(string tex) =>
        Assert.Throws<InvalidOperationException>(() => TexSafety.Validate(tex));

    [Fact]
    public void Tex_limit_is_measured_in_utf8_bytes()
    {
        var tex = new string('ą', 101);

        Assert.Throws<InvalidOperationException>(() =>
            TexSafety.Validate(tex, maxUtf8Bytes: 200));
    }
}
