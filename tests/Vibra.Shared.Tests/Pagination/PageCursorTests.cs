using Vibra.Shared.Domain;

namespace Vibra.Shared.Tests.Pagination;

public class PageCursorTests
{
    private static readonly Guid Code = new("0f8fad5b-d9cb-469f-a165-70867728950e");

    [Fact]
    public void Cursor_round_trips_and_has_a_fixed_length()
    {
        var cursor = PageCursor.Encode(CursorKind.Track, Code);

        Assert.Equal(PageCursor.Length, cursor.Length);
        Assert.True(PageCursor.TryDecode(cursor, CursorKind.Track, out var decoded));
        Assert.Equal(Code, decoded);
    }

    [Fact]
    public void Cursor_of_another_kind_is_rejected() =>
        Assert.False(PageCursor.TryDecode(PageCursor.Encode(CursorKind.Album, Code), CursorKind.Track, out _));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("!!!!!!!!!!!!!!!!!!!!!!!")]
    [InlineData("                       ")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Malformed_cursor_is_rejected(string? cursor) =>
        Assert.False(PageCursor.TryDecode(cursor, CursorKind.Track, out _));

    // El último carácter tiene bits sin usar: otras variantes darían los mismos bytes.
    [Fact]
    public void Only_the_canonical_text_is_accepted()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var cursor = PageCursor.Encode(CursorKind.Track, Code);

        var accepted = alphabet
            .Select(last => cursor[..^1] + last)
            .Where(candidate => PageCursor.TryDecode(candidate, CursorKind.Track, out var code) && code == Code);

        Assert.Equal([cursor], accepted);
    }

    [Fact]
    public void Page_keeps_the_limit_and_points_after_its_last_item()
    {
        Guid[] rows = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        var more = Page.From(rows, 2, CursorKind.Track, code => code);
        var last = Page.From(rows, 3, CursorKind.Track, code => code);

        Assert.Equal(rows[..2], more.Items);
        Assert.Equal(PageCursor.Encode(CursorKind.Track, rows[1]), more.NextCursor);
        Assert.Equal(rows, last.Items);
        Assert.Null(last.NextCursor);
    }
}
