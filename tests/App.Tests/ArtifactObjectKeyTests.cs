using App.Infrastructure;

namespace App.Tests;

public sealed class ArtifactObjectKeyTests
{
    [Fact]
    public void Creates_workspace_scoped_cloud_object_key()
    {
        var artifactId = Guid.NewGuid();

        var key = ArtifactObjectKey.Create("user:person@example.com", artifactId);

        Assert.Equal($"user-person@example.com/{artifactId:N}.pdf", key);
    }

    [Theory]
    [InlineData("../private.pdf")]
    [InlineData("/private.pdf")]
    [InlineData(@"workspace\private.pdf")]
    public void Rejects_invalid_object_keys(string key)
    {
        Assert.Throws<InvalidOperationException>(() => ArtifactObjectKey.Validate(key));
    }
}
