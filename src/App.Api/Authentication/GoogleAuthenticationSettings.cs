using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace App.Api.Authentication;

internal sealed record GoogleAllowedUser(string Email, string WorkspaceId);

internal sealed class GoogleAuthenticationSettings
{
    private static readonly Regex WorkspaceIdPattern = new(
        "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$",
        RegexOptions.CultureInvariant);

    private readonly IReadOnlyDictionary<string, string> workspaceIdsByEmail;

    private GoogleAuthenticationSettings(
        bool enabled,
        string clientId,
        string clientSecret,
        Uri frontendUrl,
        IReadOnlyDictionary<string, string> workspaceIdsByEmail)
    {
        Enabled = enabled;
        ClientId = clientId;
        ClientSecret = clientSecret;
        FrontendUrl = frontendUrl;
        this.workspaceIdsByEmail = workspaceIdsByEmail;
    }

    public bool Enabled { get; }
    public string ClientId { get; }
    public string ClientSecret { get; }
    public Uri FrontendUrl { get; }

    public bool TryGetWorkspaceId(string? email, out string workspaceId)
    {
        if (!string.IsNullOrWhiteSpace(email) &&
            workspaceIdsByEmail.TryGetValue(email.Trim(), out var configuredWorkspaceId))
        {
            workspaceId = configuredWorkspaceId;
            return true;
        }

        workspaceId = string.Empty;
        return false;
    }

    public bool IsAllowed(string? email, string? workspaceId) =>
        TryGetWorkspaceId(email, out var configuredWorkspaceId) &&
        string.Equals(configuredWorkspaceId, workspaceId, StringComparison.Ordinal);

    public string FrontendRedirect(string errorCode) =>
        QueryHelpers.AddQueryString(FrontendUrl.AbsoluteUri, "authError", errorCode);

    public static GoogleAuthenticationSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication:Google");
        var enabled = section.GetValue<bool>("Enabled");
        var clientId = section["ClientId"]?.Trim() ?? string.Empty;
        var clientSecret = section["ClientSecret"]?.Trim() ?? string.Empty;
        var frontendUrlValue = section["FrontendUrl"]?.Trim() ?? "http://localhost:5173/";
        var frontendUrl = ParseFrontendUrl(frontendUrlValue);
        var allowedUsers = section.GetSection("AllowedUsers")
            .GetChildren()
            .Select(user => new GoogleAllowedUser(
                user["Email"]?.Trim() ?? string.Empty,
                user["WorkspaceId"]?.Trim() ?? string.Empty))
            .ToArray();

        if (!enabled)
        {
            return new GoogleAuthenticationSettings(
                false,
                clientId,
                clientSecret,
                frontendUrl,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        if (clientId.Length == 0 || clientSecret.Length == 0)
        {
            throw new InvalidOperationException(
                "Authentication:Google:ClientId and ClientSecret are required when Google sign-in is enabled.");
        }

        if (allowedUsers.Length is 0 or > 10)
        {
            throw new InvalidOperationException(
                "Authentication:Google:AllowedUsers must contain between 1 and 10 users.");
        }

        var workspaceIdsByEmail = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var workspaceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var user in allowedUsers)
        {
            ValidateEmail(user.Email);
            ValidateWorkspaceId(user.WorkspaceId);

            if (!workspaceIdsByEmail.TryAdd(user.Email, user.WorkspaceId))
            {
                throw new InvalidOperationException(
                    $"Authentication:Google:AllowedUsers contains duplicate email '{user.Email}'.");
            }

            if (!workspaceIds.Add(user.WorkspaceId))
            {
                throw new InvalidOperationException(
                    $"Authentication:Google:AllowedUsers contains duplicate workspace ID '{user.WorkspaceId}'.");
            }
        }

        return new GoogleAuthenticationSettings(
            true,
            clientId,
            clientSecret,
            frontendUrl,
            workspaceIdsByEmail);
    }

    private static Uri ParseFrontendUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             (uri.Scheme != Uri.UriSchemeHttp || !uri.IsLoopback)) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException(
                "Authentication:Google:FrontendUrl must be an HTTPS URL, or an HTTP loopback URL, without a query or fragment.");
        }

        return uri;
    }

    private static void ValidateEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                $"Authentication:Google:AllowedUsers contains invalid email '{email}'.");
        }
    }

    private static void ValidateWorkspaceId(string workspaceId)
    {
        if (!WorkspaceIdPattern.IsMatch(workspaceId))
        {
            throw new InvalidOperationException(
                "Authentication:Google:AllowedUsers WorkspaceId must be 1-128 letters, digits, dots, underscores, or hyphens.");
        }
    }
}
