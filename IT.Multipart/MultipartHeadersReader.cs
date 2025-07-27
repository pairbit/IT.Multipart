using System;

namespace IT.Multipart;

public ref struct MultipartHeadersReader
{
    private const byte Sep = (byte)':';
    private const byte CR = (byte)'\r';
    private const byte LF = (byte)'\n';
    private static readonly byte[] CRLF = [CR, LF];

    private readonly ReadOnlySpan<byte> _span;
    private int _offset;

    public readonly ReadOnlySpan<byte> Span => _span;

    public readonly int Offset => _offset;

    public MultipartHeadersReader(ReadOnlySpan<byte> span)
    {
        _span = span;
        _offset = 0;
    }

    public void Reset()
    {
        _offset = 0;
    }

    public MultipartReadingStatus ReadNextHeader(out MultipartHeader header)
        => ReadNextHeader(out header, TrimOptions.MinStart);

    public MultipartReadingStatus ReadNextHeader(out MultipartHeader header, TrimOptions trimValue)
    {
        var offset = _offset;
        if (offset < 0)
        {
            header = default;
            return (MultipartReadingStatus)checked((sbyte)offset);
        }
        var span = _span;
        if (span.Length <= offset)
        {
            header = default;
            return MultipartReadingStatus.End;
        }
        span = _span.Slice(offset);
#if DEBUG
        var spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        var end = span.IndexOf(CRLF);
        if (end < 0) end = span.Length;
        else
        {
            span = span.Slice(0, end);
#if DEBUG
            spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        }
        var nameEnd = span.IndexOf(Sep);
        if (nameEnd < 0)
        {
            _offset = (int)MultipartReadingStatus.HeaderSeparatorNotFound;
            header = default;
            return MultipartReadingStatus.HeaderSeparatorNotFound;
        }
        if (nameEnd == 0)
        {
            _offset = (int)MultipartReadingStatus.HeaderNameNotFound;
            header = default;
            return MultipartReadingStatus.HeaderNameNotFound;
        }
        var valueStart = nameEnd + 1;
        if (valueStart < span.Length && trimValue.HasStart)
            trimValue.ClampStart(span, ref valueStart);
        var valueEnd = end - 1;
        if (valueEnd >= valueStart && trimValue.HasEnd)
            trimValue.ClampEnd(span, valueStart, ref valueEnd);
        header = new MultipartHeader
        {
            Name = new Range(offset, nameEnd + offset),
            Value = new Range(valueStart + offset, valueEnd + offset + 1)
        };
#if DEBUG
        var nameUtf8 = System.Text.Encoding.UTF8.GetString(_span[header.Name]);
        var valueUtf8 = System.Text.Encoding.UTF8.GetString(_span[header.Value]);
#endif
        _offset = end + offset + 2;
        return MultipartReadingStatus.Done;
    }

    public MultipartReadingStatus ReadNextHeaderValueByName(ReadOnlySpan<byte> name, out Range value)
        => ReadNextHeaderValueByName(name, out value, TrimOptions.MinStart);

    public MultipartReadingStatus ReadNextHeaderValueByName(ReadOnlySpan<byte> name, out Range value, TrimOptions trimValue)
    {
        var status = ReadNextHeader(out var header, trimValue);
        if (status != MultipartReadingStatus.Done)
        {
            value = default;
            return status;
        }
        if (!_span[header.Name].SequenceEqual(name))
        {
            value = default;
            return MultipartReadingStatus.HeaderNameNotSame;
        }
        value = header.Value;
        return MultipartReadingStatus.Done;
    }

    public MultipartReadingStatus ReadNextContentDisposition(out Range value)
        => ReadNextHeaderValueByName("Content-Disposition"u8, out value, TrimOptions.MinStart);

    public MultipartReadingStatus ReadNextContentDisposition(out Range value, TrimOptions trimValue)
        => ReadNextHeaderValueByName("Content-Disposition"u8, out value, trimValue);

    public MultipartReadingStatus ReadNextContentType(out Range value)
        => ReadNextHeaderValueByName("Content-Type"u8, out value, TrimOptions.MinStart);

    public MultipartReadingStatus ReadNextContentType(out Range value, TrimOptions trimValue)
        => ReadNextHeaderValueByName("Content-Type"u8, out value, trimValue);

    public bool TryReadNextHeader(out MultipartHeader header)
    {
        var offset = _offset;
        var span = _span;
        if (span.Length <= offset)
        {
            header = default;
            return false;
        }
        span = _span.Slice(offset);
#if DEBUG
        var spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        var end = span.IndexOf(CRLF);
        if (end < 0) end = span.Length;
        else
        {
            span = span.Slice(0, end);
#if DEBUG
            spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        }
        var nameEnd = span.IndexOf(Sep);
        if (nameEnd < 0) throw SeparatorNotFound();
        if (nameEnd == 0) throw NameNotFound();
        var valueStart = nameEnd + 1;
        for (; valueStart < span.Length; valueStart++)
        {
            if (!IsWhiteSpace(span[valueStart])) break;
        }
        var valueEnd = end - 1;
        for (; valueEnd >= valueStart; valueEnd--)
        {
            if (!IsWhiteSpace(span[valueEnd])) break;
        }
        header = new MultipartHeader
        {
            Name = new Range(offset, nameEnd + offset),
            Value = new Range(valueStart + offset, valueEnd + offset + 1)
        };
#if DEBUG
        var nameUtf8 = System.Text.Encoding.UTF8.GetString(_span[header.Name]);
        var valueUtf8 = System.Text.Encoding.UTF8.GetString(_span[header.Value]);
#endif
        _offset = end + offset + 2;
        return true;
    }

    public bool TryReadNextHeaderValueByName(ReadOnlySpan<byte> name, out Range value)
    {
        if (TryReadNextHeader(out var header))
        {
            if (_span[header.Name].SequenceEqual(name))
            {
                value = header.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public bool TryFindHeaderValueByName(ReadOnlySpan<byte> name, out Range value)
    {
        while (TryReadNextHeader(out var header))
        {
            if (_span[header.Name].SequenceEqual(name))
            {
                value = header.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public bool TryReadNextContentDisposition(out Range value)
        => TryReadNextHeaderValueByName("Content-Disposition"u8, out value);

    public bool TryReadNextContentType(out Range value)
        => TryReadNextHeaderValueByName("Content-Type"u8, out value);

    public bool TryFindContentDisposition(out Range value)
        => TryFindHeaderValueByName("Content-Disposition"u8, out value);

    public bool TryFindContentType(out Range value)
        => TryFindHeaderValueByName("Content-Type"u8, out value);

    internal static InvalidOperationException SeparatorNotFound()
        => new("Invalid header. Separator ':' not found");

    internal static InvalidOperationException NameNotFound()
        => new("Invalid header. Name not found");

    private static bool IsWhiteSpace(byte b) => b is
        (byte)' ' or //space
        (byte)'\n' or //newline
        (byte)'\r' or //carriage return
        (byte)'\t' or //horizontal tab
        (byte)'\v' or //vertical tab
        (byte)'\f'; //form feed
}