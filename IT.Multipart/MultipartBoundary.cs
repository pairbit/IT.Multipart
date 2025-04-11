using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace IT.Multipart;

public readonly ref struct MultipartBoundary
{
    private const int PrefixLength = 2;
    public readonly ReadOnlySpan<byte> _span;

    public ReadOnlySpan<byte> Span => _span;

    public MultipartBoundary(ReadOnlySpan<byte> span)
    {
        _span = span;
    }

    public static int GetMinCapacity(ReadOnlySpan<char> boundary) => boundary.Length + PrefixLength;

    public static MultipartBoundary FromStringSegment<TBufferWriter>(ReadOnlySpan<char> boundary,
        TBufferWriter writer) where TBufferWriter : IBufferWriter<byte>
    {
        boundary = RemoveQuotes(boundary);

        var length = boundary.Length;
        if (length == 0) throw new ArgumentException("boundary is empty", nameof(boundary));

        var utf8Length = Encoding.UTF8.GetByteCount(boundary) + PrefixLength;

        var span = writer.GetSpan(utf8Length).Slice(0, utf8Length);
        span[0] = (byte)'-';
        span[1] = (byte)'-';
#if NET6_0_OR_GREATER
        var status = System.Text.Unicode.Utf8.FromUtf16(boundary, span.Slice(PrefixLength), out var readed, out var written);
        if (status != OperationStatus.Done) throw new InvalidOperationException($"Status is {status}");
        Debug.Assert(length == readed);
#else
        var written = Encoding.UTF8.GetBytes(boundary, span.Slice(PrefixLength));
#endif
        Debug.Assert(utf8Length - PrefixLength == written);

        writer.Advance(utf8Length);

        return new MultipartBoundary(span);
    }

    internal static ReadOnlySpan<char> RemoveQuotes(ReadOnlySpan<char> span)
        => span.Length >= 2 && span[0] == '"' && span[span.Length - 1] == '"' ? span.Slice(1, span.Length - 2) : span;
}