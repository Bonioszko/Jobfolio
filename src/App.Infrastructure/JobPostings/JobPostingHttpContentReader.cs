using System.Text;

namespace App.Infrastructure;

internal static class JobPostingHttpContentReader
{
    public static async Task<string> ReadAsync(
        HttpContent content,
        int maxBytes,
        IReadOnlySet<string> allowedMediaTypes,
        string providerName,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > maxBytes)
        {
            throw new FormatException($"The {providerName} response exceeds the configured size limit.");
        }

        var mediaType = content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !allowedMediaTypes.Contains(mediaType))
        {
            throw new FormatException($"The {providerName} response has an unsupported content type.");
        }

        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > maxBytes)
            {
                throw new FormatException($"The {providerName} response exceeds the configured size limit.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return GetEncoding(content.Headers.ContentType?.CharSet)
            .GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
    }

    private static Encoding GetEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset)) return Encoding.UTF8;
        try
        {
            return Encoding.GetEncoding(charset.Trim(' ', '"'));
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }
}
