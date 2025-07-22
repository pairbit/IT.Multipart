using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace IT.Multipart;

public readonly struct MultipartBoundary
{
    private const int PrefixLength = 4;
    private static readonly byte[] Prefix = "\r\n--"u8.ToArray();

    public readonly ReadOnlyMemory<byte> _memory;

    public ReadOnlySpan<byte> Span => _memory.Span.Slice(2);

    public ReadOnlyMemory<byte> Memory => _memory.Slice(2);

    public ReadOnlySpan<byte> SpanWithPrefix => _memory.Span;

    public ReadOnlyMemory<byte> MemoryWithPrefix => _memory;

    public MultipartBoundary(ReadOnlyMemory<byte> boundary)
    {
        var span = boundary.Span;
        if (!span.StartsWith(Prefix)) throw new ArgumentException("Boundary must start with \\r\\n--", nameof(boundary));

        _memory = boundary;
    }

    public static int GetMinCapacity(ReadOnlySpan<char> boundary) => boundary.Length + PrefixLength;

    public static MultipartBoundary FromStringSegment<TBufferWriter>(ReadOnlySpan<char> boundary,
        TBufferWriter writer) where TBufferWriter : IBufferWriter<byte>
    {
        boundary = RemoveQuotes(boundary);

        var length = boundary.Length;
        if (length == 0) throw new ArgumentException("boundary is empty", nameof(boundary));

        var utf8Length = Encoding.UTF8.GetByteCount(boundary) + PrefixLength;

        var memory = writer.GetMemory(utf8Length).Slice(0, utf8Length);
        var span = memory.Span;
        span[0] = (byte)'\r';
        span[1] = (byte)'\n';
        span[2] = (byte)'-';
        span[3] = (byte)'-';
#if NET6_0_OR_GREATER
        var status = System.Text.Unicode.Utf8.FromUtf16(boundary, span.Slice(PrefixLength), out var readed, out var written);
        if (status != OperationStatus.Done) throw new InvalidOperationException($"Status is {status}");
        Debug.Assert(length == readed);
#else
        var written = Encoding.UTF8.GetBytes(boundary, span.Slice(PrefixLength));
#endif
        Debug.Assert(utf8Length - PrefixLength == written);

        writer.Advance(utf8Length);

        return new MultipartBoundary(memory);
    }

    internal static ReadOnlySpan<char> RemoveQuotes(ReadOnlySpan<char> span)
        => span.Length >= 2 && span[0] == '"' && span[span.Length - 1] == '"' ? span.Slice(1, span.Length - 2) : span;
}