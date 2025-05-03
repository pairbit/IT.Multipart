using System;

namespace IT.Multipart;

public ref struct MultipartReader
{
    private const byte Dash = (byte)'-';
    private const byte CR = (byte)'\r';
    private const byte LF = (byte)'\n';
    private static readonly byte[] CRLFCRLF = [CR, LF, CR, LF];

    private readonly ReadOnlySpan<byte> _span;
    private readonly MultipartBoundary _boundary;
    private int _offset;

    public readonly ReadOnlySpan<byte> Span => _span;

    public readonly MultipartBoundary Boundary => _boundary;

    public readonly int Offset => _offset;

    public MultipartReader(MultipartBoundary boundary, ReadOnlySpan<byte> span)
    {
        _boundary = boundary;
        _span = span;
    }

    public void Reset()
    {
        _offset = 0;
    }

    public bool TryReadNextSection(out MultipartSection section)
    {
        var offset = _offset;
        var span = _span;
        if (span.Length <= offset)
        {
            section = default;
            return false;
        }
        span = span.Slice(offset);
#if DEBUG
        var spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        var boundary = _boundary.Span;
        var boundaryLength = boundary.Length;
        var start = 0;
        if (offset == 0)
        {
            start = span.IndexOf(boundary);
            if (start < 0) goto invalid;
            start += boundaryLength;
            span = span.Slice(start);
            if (span.Length <= 2 || span[0] != CR || span[1] != LF) goto invalid;
            start += 2;
            span = span.Slice(2);
#if DEBUG
            spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        }
        var bodyEnd = span.IndexOf(boundary);
        if (bodyEnd < 2) goto invalid;
        var end = bodyEnd + boundaryLength + 2;
        if (end > span.Length) goto invalid;
#if DEBUG
        spanUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(0, end));
#endif
        if (!IsEndBoundary(span[end - 2], span[end - 1])) goto invalid;
        bodyEnd -= 2;
        if (span[bodyEnd] != CR || span[bodyEnd + 1] != LF) goto invalid;
        span = span.Slice(0, bodyEnd);
#if DEBUG
        spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        var index = span.IndexOf(CRLFCRLF);
        if (index < 0) throw SeparatorNotFound();
        start += offset;
        index += start;
        section = new MultipartSection
        {
            Headers = new(start, index),
            Body = new(index + 4, bodyEnd + start)
        };
#if DEBUG
        var headersUtf8 = System.Text.Encoding.UTF8.GetString(_span[section.Headers]);
        var bodyUtf8 = System.Text.Encoding.UTF8.GetString(_span[section.Body]);
#endif
        _offset = end + start;
        return true;

    invalid:
        section = default;
        _offset = _span.Length;
        return false;
    }

    public bool TryFindSectionByContentDisposition(ReadOnlySpan<byte> contentDispositionType,
        ReadOnlySpan<byte> contentDispositionName, out MultipartSection section)
    {
        var span = _span;
        while (TryReadNextSection(out section))
        {
            var headers = span[section.Headers];
#if DEBUG
            var headersUtf8 = System.Text.Encoding.UTF8.GetString(headers);
            var bodyUtf8 = System.Text.Encoding.UTF8.GetString(span[section.Body]);
#endif
            var headersReader = new MultipartHeadersReader(headers);
            while (headersReader.TryFindContentDisposition(out var value))
            {
                var contentDisposition = headers[value];
#if DEBUG
                var contentDispositionUtf8 = System.Text.Encoding.UTF8.GetString(contentDisposition);
#endif
                var headerFieldsReader = new MultipartHeaderFieldsReader(contentDisposition);
                if (headerFieldsReader.TryReadNextValue(out var type))
                {
                    if (contentDisposition[type].SequenceEqual(contentDispositionType))
                    {
                        while (headerFieldsReader.TryFindValueByName("name"u8, out var name))
                        {
                            if (contentDisposition[name].SequenceEqual(contentDispositionName))
                                return true;
                        }
                    }
                }
            }
        }
        section = default;
        return false;
    }

    public bool TryFindSectionByContentDispositionFormData(ReadOnlySpan<byte> name, out MultipartSection section)
        => TryFindSectionByContentDisposition("form-data"u8, name, out section);

    internal static InvalidOperationException SeparatorNotFound()
        => new("Invalid multipart section. Separator '\\r\\n\\r\\n' not found");

    private static bool IsEndBoundary(byte first, byte second)
        => first == Dash && second == Dash ||
           first == CR && second == LF;
}