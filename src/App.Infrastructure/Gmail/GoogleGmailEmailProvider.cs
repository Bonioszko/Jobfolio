using System.Text;
using System.Text.RegularExpressions;
using App.Application;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Util;

namespace App.Infrastructure;

internal sealed class GoogleGmailEmailProvider(GmailOAuthSettings settings)
    : IEmailProvider, IDisposable
{
    private readonly SemaphoreSlim initializationGate = new(1, 1);
    private GmailService? service;

    public async Task<IReadOnlyList<string>> ListMessageIdsByLabelsAsync(
        IReadOnlyList<string> labels,
        int maxMessages,
        CancellationToken cancellationToken)
    {
        if (labels.Count == 0) throw new ArgumentException("At least one Gmail label is required.", nameof(labels));
        if (maxMessages is <= 0 or > 500) throw new ArgumentOutOfRangeException(nameof(maxMessages));

        var gmail = await GetServiceAsync(cancellationToken);
        var labelIds = await ResolveLabelIdsAsync(gmail, labels, cancellationToken);
        return await ListMessageIdsAsync(gmail, labelIds, maxMessages, cancellationToken);
    }

    public async Task<EmailMessage> GetMessageAsync(
        string messageId,
        int maxBodyBytes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentException("A Gmail message ID is required.", nameof(messageId));
        if (maxBodyBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBodyBytes));

        var gmail = await GetServiceAsync(cancellationToken);
        var request = gmail.Users.Messages.Get("me", messageId);
        request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
        var message = await request.ExecuteAsync(cancellationToken);
        return await GmailMessageConverter.ConvertAsync(gmail, message, maxBodyBytes, cancellationToken);
    }

    private async Task<GmailService> GetServiceAsync(CancellationToken cancellationToken)
    {
        if (service is not null) return service;

        await initializationGate.WaitAsync(cancellationToken);
        try
        {
            if (service is not null) return service;
            var secrets = await GetClientSecretsAsync(cancellationToken);
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = [GmailService.Scope.GmailReadonly]
            };
            UserCredential credential;
            if (settings.UsesRefreshToken)
            {
                var flow = new GoogleAuthorizationCodeFlow(initializer);
                credential = new UserCredential(
                    flow,
                    settings.UserKey,
                    new TokenResponse { RefreshToken = settings.RefreshToken });
                if (!await credential.RefreshTokenAsync(cancellationToken))
                {
                    throw new InvalidOperationException("The configured Gmail refresh token was rejected.");
                }
            }
            else
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    initializer,
                    [GmailService.Scope.GmailReadonly],
                    settings.UserKey,
                    true,
                    cancellationToken,
                    new FileDataStore(settings.TokenStoreDirectory, fullPath: true));
            }
            var initializedService = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Job Parser Gmail Sync"
            });

            try
            {
                await ValidateAuthorizedAccountAsync(initializedService, cancellationToken);
                service = initializedService;
                return service;
            }
            catch
            {
                initializedService.Dispose();
                throw;
            }
        }
        finally
        {
            initializationGate.Release();
        }
    }

    private async Task ValidateAuthorizedAccountAsync(
        GmailService gmail,
        CancellationToken cancellationToken)
    {
        if (settings.ExpectedEmail is null)
        {
            return;
        }

        var profile = await gmail.Users.GetProfile("me").ExecuteAsync(cancellationToken);
        EnsureExpectedAccount(settings.ExpectedEmail, profile.EmailAddress);
    }

    internal static void EnsureExpectedAccount(string expectedEmail, string? authorizedEmail)
    {
        if (!string.Equals(expectedEmail, authorizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The authorized Gmail account does not match Gmail:AccountEmail.");
        }
    }

    private async Task<ClientSecrets> GetClientSecretsAsync(CancellationToken cancellationToken)
    {
        if (settings.UsesRefreshToken)
        {
            var secrets = new ClientSecrets
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret
            };
            ValidateClientSecrets(secrets);
            return secrets;
        }

        ValidateClientSecretsFile(settings.ClientSecretsPath!);
        var clientSecrets = await GoogleClientSecrets.FromFileAsync(
            settings.ClientSecretsPath!,
            cancellationToken);
        ValidateClientSecrets(clientSecrets.Secrets);
        return clientSecrets.Secrets;
    }

    private static async Task<IReadOnlyList<string>> ResolveLabelIdsAsync(
        GmailService gmail,
        IReadOnlyList<string> configuredLabels,
        CancellationToken cancellationToken)
    {
        var response = await gmail.Users.Labels.List("me").ExecuteAsync(cancellationToken);
        var available = response.Labels ?? [];
        var resolved = configuredLabels.Select(configured => available.SingleOrDefault(label =>
                string.Equals(label.Id, configured, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(label.Name, configured, StringComparison.OrdinalIgnoreCase))?.Id
            ?? throw new InvalidOperationException($"Configured Gmail label '{configured}' was not found."))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return resolved;
    }

    private static async Task<IReadOnlyList<string>> ListMessageIdsAsync(
        GmailService gmail,
        IReadOnlyList<string> labelIds,
        int maxMessages,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var labelId in labelIds)
        {
            string? pageToken = null;
            do
            {
                var request = gmail.Users.Messages.List("me");
                request.LabelIds = new Repeatable<string>([labelId]);
                request.IncludeSpamTrash = false;
                request.MaxResults = Math.Min(500, maxMessages - ids.Count);
                request.PageToken = pageToken;
                var response = await request.ExecuteAsync(cancellationToken);
                foreach (var message in response.Messages ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(message.Id)) ids.Add(message.Id);
                    if (ids.Count == maxMessages) break;
                }

                pageToken = ids.Count < maxMessages ? response.NextPageToken : null;
            } while (!string.IsNullOrWhiteSpace(pageToken));

            if (ids.Count == maxMessages) break;
        }

        return ids.ToArray();
    }

    private static void ValidateClientSecretsFile(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("The configured Gmail OAuth client file was not found.", path);
        if (file.Length is <= 0 or > 65_536)
        {
            throw new InvalidOperationException("The Gmail OAuth client file has an invalid size.");
        }
    }

    private static void ValidateClientSecrets(ClientSecrets secrets)
    {
        if (string.IsNullOrWhiteSpace(secrets.ClientId) ||
            !secrets.ClientId.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal) ||
            secrets.ClientId.Length > 512 ||
            string.IsNullOrWhiteSpace(secrets.ClientSecret) ||
            secrets.ClientSecret.Length > 512)
        {
            throw new InvalidOperationException("The Gmail OAuth client file is not a valid Google client configuration.");
        }
    }

    public void Dispose()
    {
        service?.Dispose();
        initializationGate.Dispose();
    }
}

internal static partial class GmailMessageConverter
{
    public static async Task<EmailMessage> ConvertAsync(
        GmailService gmail,
        Message message,
        int maxBodyBytes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Id) || message.Payload is null)
        {
            throw new FormatException("Gmail returned a message without an ID or payload.");
        }

        var accumulator = new BodyAccumulator(maxBodyBytes);
        await CollectBodiesAsync(gmail, message.Id, message.Payload, accumulator, cancellationToken);
        var html = NullIfEmpty(string.Join('\n', accumulator.HtmlBodies));
        var text = NullIfEmpty(string.Join('\n', accumulator.TextBodies));
        if (html is null && text is null)
        {
            throw new FormatException("The Gmail message does not contain a readable text body.");
        }

        var headers = message.Payload.Headers ?? [];
        return new EmailMessage(
            message.Id,
            GetHeader(headers, "From") ?? throw new FormatException("The Gmail message has no From header."),
            GetHeader(headers, "Subject") ?? string.Empty,
            html ?? text!,
            GetReceivedAt(message),
            text);
    }

    private static async Task CollectBodiesAsync(
        GmailService gmail,
        string messageId,
        MessagePart part,
        BodyAccumulator accumulator,
        CancellationToken cancellationToken)
    {
        var mimeType = part.MimeType?.Split(';', 2)[0].Trim();
        if (mimeType is "text/html" or "text/plain")
        {
            var encoded = part.Body?.Data;
            if (encoded is null && !string.IsNullOrWhiteSpace(part.Body?.AttachmentId))
            {
                var attachment = await gmail.Users.Messages.Attachments
                    .Get("me", messageId, part.Body.AttachmentId)
                    .ExecuteAsync(cancellationToken);
                encoded = attachment.Data;
            }

            if (!string.IsNullOrWhiteSpace(encoded))
            {
                accumulator.EnsureEncodedBodyFits(encoded.Length);
                var bytes = DecodeBase64Url(encoded);
                accumulator.Add(mimeType, DecodeText(bytes, part.Headers));
            }
        }

        foreach (var child in part.Parts ?? [])
        {
            await CollectBodiesAsync(gmail, messageId, child, accumulator, cancellationToken);
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
        try
        {
            return Convert.FromBase64String(normalized);
        }
        catch (FormatException exception)
        {
            throw new FormatException("The Gmail message contains an invalid encoded body.", exception);
        }
    }

    private static string DecodeText(byte[] bytes, IList<MessagePartHeader>? headers)
    {
        var contentType = GetHeader(headers ?? [], "Content-Type");
        var charset = contentType is null ? null : CharsetRegex().Match(contentType).Groups["charset"].Value;
        try
        {
            return string.IsNullOrWhiteSpace(charset)
                ? Encoding.UTF8.GetString(bytes)
                : Encoding.GetEncoding(charset.Trim(' ', '"', '\''),
                    EncoderFallback.ReplacementFallback,
                    DecoderFallback.ReplacementFallback).GetString(bytes);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }

    private static DateTimeOffset GetReceivedAt(Message message) =>
        message.InternalDate is { } milliseconds
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds)
            : DateTimeOffset.UtcNow;

    private static string? GetHeader(IEnumerable<MessagePartHeader> headers, string name) =>
        headers.FirstOrDefault(header => header.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class BodyAccumulator(int maxBytes)
    {
        private int totalBytes;
        public List<string> HtmlBodies { get; } = [];
        public List<string> TextBodies { get; } = [];

        public void EnsureEncodedBodyFits(int encodedLength)
        {
            var approximateDecodedBytes = (encodedLength * 3L) / 4L;
            if (totalBytes + approximateDecodedBytes > maxBytes)
            {
                throw new FormatException("The Gmail message body exceeds the configured size limit.");
            }
        }

        public void Add(string mimeType, string value)
        {
            totalBytes = checked(totalBytes + Encoding.UTF8.GetByteCount(value));
            if (totalBytes > maxBytes)
            {
                throw new FormatException("The Gmail message body exceeds the configured size limit.");
            }

            (mimeType == "text/html" ? HtmlBodies : TextBodies).Add(value);
        }
    }

    [GeneratedRegex("charset\\s*=\\s*(?<charset>[^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CharsetRegex();
}
