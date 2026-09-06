using App.Api.Endpoints.JobPostings;
using App.Application;

namespace App.Api.Tests;

public sealed class JobPostingCursorCodecTests
{
    [Fact]
    public void Cursor_round_trips_without_losing_sort_values()
    {
        var expected = new JobPostingCursor(
            DateTimeOffset.Parse("2026-09-06T08:42:17.1234567+02:00"),
            Guid.Parse("a8d313c8-85b0-48e8-bffc-125535f4fbe1"));

        var encoded = JobPostingCursorCodec.Encode(expected);
        var decoded = JobPostingCursorCodec.TryDecode(encoded, out var actual);

        Assert.True(decoded);
        Assert.Equal(expected.SourceReceivedAt.ToUniversalTime(), actual.SourceReceivedAt);
        Assert.Equal(expected.Id, actual.Id);
    }

    [Theory]
    [InlineData("not-base64!")]
    [InlineData("c2hvcnQ")]
    [InlineData("")]
    public void Invalid_cursor_is_rejected(string value)
    {
        Assert.False(JobPostingCursorCodec.TryDecode(value, out _));
    }
}
