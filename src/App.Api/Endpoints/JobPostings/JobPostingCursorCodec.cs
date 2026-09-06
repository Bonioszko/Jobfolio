using System.Buffers.Binary;
using App.Application;
using Microsoft.AspNetCore.WebUtilities;

namespace App.Api.Endpoints.JobPostings;

internal static class JobPostingCursorCodec
{
    private const int CursorLength = sizeof(long) + 16;
    private const int EncodedCursorLength = 32;

    public static string Encode(JobPostingCursor cursor)
    {
        Span<byte> payload = stackalloc byte[CursorLength];
        BinaryPrimitives.WriteInt64BigEndian(
            payload,
            cursor.SourceReceivedAt.UtcDateTime.Ticks);
        cursor.Id.TryWriteBytes(payload[sizeof(long)..]);
        return WebEncoders.Base64UrlEncode(payload.ToArray());
    }

    public static bool TryDecode(string value, out JobPostingCursor cursor)
    {
        cursor = default!;

        if (value.Length != EncodedCursorLength)
        {
            return false;
        }

        byte[] payload;
        try
        {
            payload = WebEncoders.Base64UrlDecode(value);
        }
        catch (FormatException)
        {
            return false;
        }

        if (payload.Length != CursorLength)
        {
            return false;
        }

        var ticks = BinaryPrimitives.ReadInt64BigEndian(payload);
        if (ticks < DateTimeOffset.MinValue.UtcDateTime.Ticks ||
            ticks > DateTimeOffset.MaxValue.UtcDateTime.Ticks)
        {
            return false;
        }

        cursor = new JobPostingCursor(
            new DateTimeOffset(ticks, TimeSpan.Zero),
            new Guid(payload.AsSpan(sizeof(long))));
        return true;
    }
}
