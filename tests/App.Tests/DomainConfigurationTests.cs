using App.Application;

namespace App.Tests;

public sealed class DomainConfigurationTests
{
    [Fact]
    public void Duplicate_status_codes_are_rejected()
    {
        var config = new DomainConfiguration(new("Item", "Items"), new("Document", "Documents"), [new("title", "Title", "string", true)], [new("NEW", "New"), new("new", "Again")]);
        Assert.Throws<InvalidOperationException>(config.Validate);
    }
}
