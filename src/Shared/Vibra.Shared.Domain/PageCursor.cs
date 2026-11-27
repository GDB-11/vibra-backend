using System.Buffers.Text;

namespace Vibra.Shared.Domain;

/// <summary>Tipo de recurso de un cursor. Los valores viajan dentro del cursor: no los cambies.</summary>
public enum CursorKind : byte
{
    Artist = 1,
    Album = 2,
    Track = 3,
}

/// <summary>
/// Cursor opaco: el tipo de recurso y el código público del último elemento de la página.
/// Nunca contiene ids internos.
/// </summary>
public static class PageCursor
{
    private const int GuidSize = 16;
    private const int ByteCount = 1 + GuidSize;

    /// <summary>Longitud exacta de un cursor.</summary>
    public const int Length = 23;

    /// <summary>Crea el cursor que continúa después del elemento indicado.</summary>
    public static string Encode(CursorKind kind, Guid code)
    {
        Span<byte> bytes = stackalloc byte[ByteCount];
        bytes[0] = (byte)kind;
        code.TryWriteBytes(bytes[1..], bigEndian: true, out _);

        return Base64Url.EncodeToString(bytes);
    }

    /// <summary>Lee un cursor. Solo admite el texto exacto que genera <see cref="Encode"/> para ese tipo.</summary>
    public static bool TryDecode(string? cursor, CursorKind kind, out Guid code)
    {
        code = Guid.Empty;

        if (cursor is not { Length: Length } || !Base64Url.IsValid(cursor))
            return false;

        var bytes = Base64Url.DecodeFromChars(cursor);

        if (bytes.Length != ByteCount || bytes[0] != (byte)kind)
            return false;

        var decoded = new Guid(bytes.AsSpan(1), bigEndian: true);

        // Descarta variantes del mismo valor (relleno, espacios o bits sobrantes).
        if (!string.Equals(Encode(kind, decoded), cursor, StringComparison.Ordinal))
            return false;

        code = decoded;
        return true;
    }
}
