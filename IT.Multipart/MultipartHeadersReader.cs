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
    private MultipartReadingStatus _status;

    public readonly ReadOnlySpan<byte> Span => _span;

    public readonly int Offset => _offset;

    public readonly MultipartReadingStatus Status => _status;

    public MultipartHeadersReader(ReadOnlySpan<byte> span)
    {
        _span = span;
        _offset = 0;
    }

    public void Reset()
    {
        _offset = 0;
        _status = MultipartReadingStatus.Done;
    }

    public bool ReadNextHeader(out MultipartHeader header)
        => ReadNextHeader(out header, TrimOptions.MinStart);

    public bool ReadNextHeader(out MultipartHeader header, TrimOptions trimValue)
    {
        var offset = _offset;
        var span = _span;
        if (span.Length <= offset)
        {
            header = default;
            _status = offset == 0 ? MultipartReadingStatus.HeadersNotFound : MultipartReadingStatus.End;
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
        if (nameEnd < 0)
        {
            _status = MultipartReadingStatus.HeaderSeparatorNotFound;
            header = default;
            return false;
        }
        if (nameEnd == 0)
        {
            _status = MultipartReadingStatus.HeaderNameNotFound;
            header = default;
            return false;
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
        _status = MultipartReadingStatus.Done;
        return true;
    }

    public bool ReadNextHeaderValueByName(ReadOnlySpan<byte> name, out Range value)
        => ReadNextHeaderValueByName(name, out value, TrimOptions.MinStart);

    public bool ReadNextHeaderValueByName(ReadOnlySpan<byte> name, out Range value, TrimOptions trimValue)
        => ReadNextHeaderValueByName(name, out value, trimValue, MultipartReadingStatus.HeaderNameNotSame);

    public bool ReadNextContentDisposition(out Range value)
        => ReadNextHeaderValueByName("Content-Disposition"u8, out value, TrimOptions.MinStart, MultipartReadingStatus.HeaderContentDispositionNotFound);

    public bool ReadNextContentDisposition(out Range value, TrimOptions trimValue)
        => ReadNextHeaderValueByName("Content-Disposition"u8, out value, trimValue, MultipartReadingStatus.HeaderContentDispositionNotFound);

    public bool ReadNextContentType(out Range value)
        => ReadNextHeaderValueByName("Content-Type"u8, out value, TrimOptions.MinStart, MultipartReadingStatus.HeaderContentTypeNotFound);

    public bool ReadNextContentType(out Range value, TrimOptions trimValue)
        => ReadNextHeaderValueByName("Content-Type"u8, out value, trimValue, MultipartReadingStatus.HeaderContentTypeNotFound);

    public bool FindHeaderValueByName(ReadOnlySpan<byte> name, out Range value, TrimOptions trimValue)
    {
        while (ReadNextHeader(out var header, trimValue))
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

    public bool FindContentDisposition(out Range value)
        => FindHeaderValueByName("Content-Disposition"u8, out value, TrimOptions.MinStart);

    public bool FindContentDisposition(out Range value, TrimOptions trimValue)
        => FindHeaderValueByName("Content-Disposition"u8, out value, trimValue);

    public bool FindContentType(out Range value)
        => FindHeaderValueByName("Content-Type"u8, out value, TrimOptions.MinStart);

    public bool FindContentType(out Range value, TrimOptions trimValue)
        => FindHeaderValueByName("Content-Type"u8, out value, trimValue);

    private bool ReadNextHeaderValueByName(ReadOnlySpan<byte> name, out Range value, TrimOptions trimValue,
        MultipartReadingStatus headerNameNotSame)
    {
        if (!ReadNextHeader(out var header, trimValue))
        {
            value = default;
            return false;
        }
        if (!_span[header.Name].SequenceEqual(name))
        {
            value = default;
            _status = headerNameNotSame;
            return false;
        }
        value = header.Value;
        return true;
    }
}