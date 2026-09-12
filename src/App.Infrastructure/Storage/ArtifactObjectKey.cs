namespace App.Infrastructure;

internal static class ArtifactObjectKey
{
    public static string Create(string workspaceKey, Guid artifactId) =>
        $"{GetWorkspacePrefix(workspaceKey)}/{artifactId:N}.pdf";

    public static string GetWorkspacePrefix(string workspaceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceKey);

        if (workspaceKey.StartsWith("demo:", StringComparison.Ordinal) &&
            Guid.TryParse(workspaceKey["demo:".Length..], out _))
        {
            return workspaceKey.Replace(':', '-');
        }

        if (workspaceKey.StartsWith("user:", StringComparison.Ordinal))
        {
            var userId = workspaceKey["user:".Length..];
            var isSafe = userId.Length is > 0 and <= 200 && userId.All(character =>
                char.IsLetterOrDigit(character) || character is '.' or '_' or '@' or '+' or '-');
            if (isSafe) return workspaceKey.Replace(':', '-');
        }

        throw new InvalidOperationException(
            "The workspace key cannot be mapped to artifact storage.");
    }

    public static void Validate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.StartsWith("/", StringComparison.Ordinal) ||
            key.Contains(@"\", StringComparison.Ordinal) ||
            key.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException("The artifact object key is invalid.");
        }
    }
}
