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

    public MultipartReadingStatus ReadNextSection(out MultipartSection section, bool isStrict = true)
    {
        var offset = _offset;
        if (offset < 0)
        {
            section = default;
            return (MultipartReadingStatus)checked((sbyte)offset);
        }
        var span = _span;
        if (span.Length <= offset)
        {
            section = default;
            return offset == 0 ? MultipartReadingStatus.SectionsNotFound : MultipartReadingStatus.End;
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
                if (!span.StartsWith(boundary.Slice(2)))
                {
                    _offset = (int)MultipartReadingStatus.StartBoundaryNotFound;
                    section = default;
                    return MultipartReadingStatus.StartBoundaryNotFound;
                }
            }
            else
            {
                start = span.IndexOf(boundary.Slice(2));//del \r\n
                if (start < 0)
                {
                    _offset = (int)MultipartReadingStatus.StartBoundaryNotFound;
                    section = default;
                    return MultipartReadingStatus.StartBoundaryNotFound;
                }
            }

            start += boundaryLength - 2;
            span = span.Slice(start);
            if (span.Length <= 2 || span[0] != CR || span[1] != LF)
            {
                _offset = (int)MultipartReadingStatus.StartBoundaryCRLFNotFound;
                section = default;
                return MultipartReadingStatus.StartBoundaryCRLFNotFound;
            }
            start += 2;
            span = span.Slice(2);
#if DEBUG
            spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        }
        var bodyEnd = span.IndexOf(boundary);
        if (bodyEnd < 0)
        {
            _offset = (int)MultipartReadingStatus.BoundaryNotFound;
            section = default;
            return MultipartReadingStatus.BoundaryNotFound;
        }
        var end = bodyEnd + boundaryLength + 2;
        if (end > span.Length)
        {
            _offset = (int)MultipartReadingStatus.EndBoundaryNotFound;
            section = default;
            return MultipartReadingStatus.EndBoundaryNotFound;
        }
#if DEBUG
        spanUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(0, end));
#endif
        var first = span[end - 2];
        var second = span[end - 1];
        if (first != CR || second != LF)
        {
            if (first != Dash || second != Dash)
            {
                _offset = (int)MultipartReadingStatus.EndBoundaryNotFound;
                section = default;
                return MultipartReadingStatus.EndBoundaryNotFound;
            }
            if (isStrict)
            {
                end += 2;
                if (end != span.Length || span[end - 2] != CR || span[end - 1] != LF)
                {
                    _offset = (int)MultipartReadingStatus.EndBoundaryNotFound;
                    section = default;
                    return MultipartReadingStatus.EndBoundaryNotFound;
                }
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
        if (index < 0)
        {
            _offset = (int)MultipartReadingStatus.SectionSeparatorNotFound;
            section = default;
            return MultipartReadingStatus.SectionSeparatorNotFound;
        }
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
        return MultipartReadingStatus.Done;
    }

    public MultipartReadingStatus ReadNextSectionByContentDisposition(ReadOnlySpan<byte> contentDispositionType,
        ReadOnlySpan<byte> contentDispositionName, out MultipartSection section, bool isStrict = true)
    {
        var status = ReadNextSection(out section, isStrict);
        if (status != MultipartReadingStatus.Done)
        {
            section = default;
            return status;
        }
        var headers = _span[section.Headers];
#if DEBUG
        var headersUtf8 = System.Text.Encoding.UTF8.GetString(headers);
        var bodyUtf8 = System.Text.Encoding.UTF8.GetString(_span[section.Body]);
#endif
        var headersReader = new MultipartHeadersReader(headers);
        status = headersReader.ReadNextContentDisposition(out var value);
        if (status != MultipartReadingStatus.Done)
        {
            section = default;
            return status;
        }
        var contentDisposition = headers[value];
#if DEBUG
        var contentDispositionUtf8 = System.Text.Encoding.UTF8.GetString(contentDisposition);
#endif
        var contentDispositionReader = new MultipartContentDispositionReader(contentDisposition);
        if (!contentDispositionReader.TryReadType(out var type))
        {
            section = default;
            return MultipartReadingStatus.HeaderFieldContentDispositionTypeNotFound;
        }
        if (!contentDisposition[type].SequenceEqual(contentDispositionType))
        {
            section = default;
            return MultipartReadingStatus.HeaderFieldContentDispositionTypeNotSame;
        }
        status = contentDispositionReader.ReadName(out var name);
        if (status != MultipartReadingStatus.Done)
        {
            //TODO: status может быть равен End
            section = default;
            return status;
        }
        if (!contentDisposition[name].SequenceEqual(contentDispositionName))
        {
            section = default;
            return MultipartReadingStatus.HeaderFieldContentDispositionNameNotSame;
        }
        return MultipartReadingStatus.Done;
    }

    public MultipartReadingStatus ReadNextSectionByContentDispositionFormData(ReadOnlySpan<byte> name, out MultipartSection section)
        => ReadNextSectionByContentDisposition("form-data"u8, name, out section);

    public MultipartReadingStatus FindSectionByContentDisposition(ReadOnlySpan<byte> contentDispositionType,
        ReadOnlySpan<byte> contentDispositionName, out MultipartSection section)
    {
        var span = _span;
        while (ReadNextSection(out section) == MultipartReadingStatus.Done)
        {
            var headers = span[section.Headers];
#if DEBUG
            var headersUtf8 = System.Text.Encoding.UTF8.GetString(headers);
            var bodyUtf8 = System.Text.Encoding.UTF8.GetString(span[section.Body]);
#endif
            var headersReader = new MultipartHeadersReader(headers);
            while (headersReader.FindContentDisposition(out var value) == MultipartReadingStatus.Done)
            {
                var contentDisposition = headers[value];
#if DEBUG
                var contentDispositionUtf8 = System.Text.Encoding.UTF8.GetString(contentDisposition);
#endif
                var contentDispositionReader = new MultipartContentDispositionReader(contentDisposition);
                if (contentDispositionReader.IsType(contentDispositionType))
                {
                    while (contentDispositionReader.FindName(out var name) == MultipartReadingStatus.Done)
                    {
                        if (contentDisposition[name].SequenceEqual(contentDispositionName))
                            return MultipartReadingStatus.Done;
                    }
                }
            }
        }
        section = default;
        return MultipartReadingStatus.HeaderNameNotSame;
    }

    public MultipartReadingStatus FindSectionByContentDispositionFormData(ReadOnlySpan<byte> name, out MultipartSection section)
        => FindSectionByContentDisposition("form-data"u8, name, out section);
}