using System;
using System.Diagnostics;

namespace IT.Multipart;

public ref struct MultipartReader
{
    private const byte Dash = (byte)'-';
    private const byte CR = (byte)'\r';
    private const byte LF = (byte)'\n';
    internal static readonly byte[] CRLFCRLF = [CR, LF, CR, LF];

    private readonly ReadOnlySpan<byte> _span;
    private readonly ReadOnlySpan<byte> _boundary;
    private int _offset;

    public readonly ReadOnlySpan<byte> Span => _span;

    public readonly ReadOnlySpan<byte> Boundary => _boundary;

    public readonly int Offset => _offset;

    public MultipartReader(MultipartBoundary boundary, ReadOnlySpan<byte> span)
    {
        _boundary = boundary.Span;
        _span = span;
    }

    public MultipartReader(ReadOnlySpan<byte> boundary, ReadOnlySpan<byte> span)
    {
        if (!MultipartBoundary.IsValid(boundary)) throw new ArgumentException("Boundary is invalid", nameof(boundary));

        _boundary = boundary;
        _span = span;
    }

    public void Reset()
    {
        _offset = 0;
    }

    public bool TryReadNextSection(out MultipartSection section, bool isStrict = true)
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
        var boundary = _boundary;
        var boundaryLength = boundary.Length;
        var start = 0;
        if (offset == 0)
        {
            Debug.Assert(boundaryLength > 2);
            if (isStrict)
            {
                if (!span.StartsWith(boundary.Slice(2))) goto invalid;
            }
            else
            {
                start = span.IndexOf(boundary.Slice(2));//del \r\n
                if (start < 0) goto invalid;
            }

            start += boundaryLength - 2;
            span = span.Slice(start);
            if (span.Length <= 2 || span[0] != CR || span[1] != LF) goto invalid;
            start += 2;
            span = span.Slice(2);
#if DEBUG
            spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        }
        var bodyEnd = span.IndexOf(boundary);
        if (bodyEnd < 0) goto invalid;
        var end = bodyEnd + boundaryLength + 2;
        if (end > span.Length) goto invalid;
#if DEBUG
        spanUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(0, end));
#endif
        var first = span[end - 2];
        var second = span[end - 1];
        if (first != CR || second != LF)
        {
            if (first != Dash || second != Dash) goto invalid;
            if (isStrict)
            {
                end += 2;
                if (end != span.Length) goto invalid;
                if (span[end - 2] != CR || span[end - 1] != LF) goto invalid;
            }
            else
            {
                end = span.Length;
            }
        }
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

    public bool TryReadNextSectionByContentDisposition(ReadOnlySpan<byte> contentDispositionType,
        ReadOnlySpan<byte> contentDispositionName, out MultipartSection section)
    {
        var span = _span;
        if (TryReadNextSection(out section))
        {
            var headers = span[section.Headers];
#if DEBUG
            var headersUtf8 = System.Text.Encoding.UTF8.GetString(headers);
            var bodyUtf8 = System.Text.Encoding.UTF8.GetString(span[section.Body]);
#endif
            var headersReader = new MultipartHeadersReader(headers);
            if (headersReader.TryReadNextContentDisposition(out var value))
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
                        if (headerFieldsReader.TryReadNextValueByName("name"u8, out var name))
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

    public bool TryReadNextSectionByContentDispositionFormData(ReadOnlySpan<byte> name, out MultipartSection section)
        => TryReadNextSectionByContentDisposition("form-data"u8, name, out section);

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
}