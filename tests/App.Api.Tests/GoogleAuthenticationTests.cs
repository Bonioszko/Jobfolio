using System.Security.Claims;
using App.Api.Authentication;
using App.Domain;
using Microsoft.Extensions.Configuration;

namespace App.Api.Tests;

public sealed class GoogleAuthenticationTests
{
    [Fact]
    public void Disabled_authentication_does_not_require_credentials_or_users()
    {
        var settings = GoogleAuthenticationSettings.FromConfiguration(BuildConfiguration([]));

        Assert.False(settings.Enabled);
    }

    [Fact]
    public void Enabled_authentication_requires_credentials_and_an_allowed_user()
    {
        var missingCredentials = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Google:Enabled"] = "true"
        });
        var missingUsers = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Google:Enabled"] = "true",
            ["Authentication:Google:ClientId"] = "client",
            ["Authentication:Google:ClientSecret"] = "secret"
        });

        Assert.Throws<InvalidOperationException>(() =>
            GoogleAuthenticationSettings.FromConfiguration(missingCredentials));
        Assert.Throws<InvalidOperationException>(() =>
            GoogleAuthenticationSettings.FromConfiguration(missingUsers));
    }

    [Fact]
    public void Verified_allowlisted_identity_maps_to_the_configured_existing_workspace()
    {
        var settings = CreateEnabledSettings();
        var googlePrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "google-subject"),
            new Claim("email", "OWNER@example.com"),
            new Claim("email_verified", "true"),
            new Claim("name", "Owner")
        ], "Google"));

        var result = GoogleClaimsPrincipalFactory.Create(googlePrincipal, settings);

        Assert.NotNull(result);
        Assert.Equal("user-one", result.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("OWNER@example.com", result.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(nameof(UserMode.Real), result.FindFirstValue("mode"));
        Assert.Equal("google-subject", result.FindFirstValue("google_subject"));
    }

    [Theory]
    [InlineData("false", "owner@example.com")]
    [InlineData("true", "someone-else@example.com")]
    public void Unverified_or_unlisted_identity_is_rejected(string emailVerified, string email)
    {
        var settings = CreateEnabledSettings();
        var googlePrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "google-subject"),
            new Claim("email", email),
            new Claim("email_verified", emailVerified)
        ], "Google"));

        var result = GoogleClaimsPrincipalFactory.Create(googlePrincipal, settings);

        Assert.Null(result);
    }

    private static GoogleAuthenticationSettings CreateEnabledSettings() =>
        GoogleAuthenticationSettings.FromConfiguration(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Google:Enabled"] = "true",
            ["Authentication:Google:ClientId"] = "client",
            ["Authentication:Google:ClientSecret"] = "secret",
            ["Authentication:Google:AllowedUsers:0:Email"] = "owner@example.com",
            ["Authentication:Google:AllowedUsers:0:WorkspaceId"] = "user-one"
        }));

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
