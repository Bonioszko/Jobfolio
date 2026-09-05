using System.Text;
using App.Infrastructure;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

namespace App.Tests;

public sealed class GmailMessageConverterTests
{
    [Fact]
    public async Task Converter_extracts_nested_html_text_headers_and_received_time()
    {
        var receivedAt = new DateTimeOffset(2026, 9, 5, 8, 30, 0, TimeSpan.Zero);
        var message = new Message
        {
            Id = "gmail-message-1",
            InternalDate = receivedAt.ToUnixTimeMilliseconds(),
            Payload = new MessagePart
            {
                MimeType = "multipart/alternative",
                Headers =
                [
                    new MessagePartHeader { Name = "From", Value = "Jobs <jobs@linkedin.com>" },
                    new MessagePartHeader { Name = "Subject", Value = "New jobs" }
                ],
                Parts =
                [
                    BodyPart("text/plain", "Plain job alert"),
                    BodyPart("text/html", "<html><body>HTML job alert</body></html>")
                ]
            }
        };

        using var gmail = new GmailService();
        var converted = await GmailMessageConverter.ConvertAsync(
            gmail,
            message,
            10_000,
            CancellationToken.None);

        Assert.Equal("gmail-message-1", converted.ExternalId);
        Assert.Equal("Jobs <jobs@linkedin.com>", converted.Sender);
        Assert.Equal("New jobs", converted.Subject);
        Assert.Contains("HTML job alert", converted.HtmlBody);
        Assert.Equal("Plain job alert", converted.TextBody);
        Assert.Equal(receivedAt, converted.ReceivedAt);
    }

    [Fact]
    public async Task Converter_rejects_bodies_over_the_configured_limit()
    {
        var message = new Message
        {
            Id = "gmail-message-2",
            Payload = new MessagePart
            {
                MimeType = "text/plain",
                Headers = [new MessagePartHeader { Name = "From", Value = "jobs@linkedin.com" }],
                Body = new MessagePartBody { Data = Encode("Too large") }
            }
        };

        using var gmail = new GmailService();
        await Assert.ThrowsAsync<FormatException>(() => GmailMessageConverter.ConvertAsync(
            gmail,
            message,
            3,
            CancellationToken.None));
    }

    private static MessagePart BodyPart(string mimeType, string body) => new()
    {
        MimeType = mimeType,
        Headers = [new MessagePartHeader { Name = "Content-Type", Value = $"{mimeType}; charset=utf-8" }],
        Body = new MessagePartBody { Data = Encode(body) }
    };

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}
