using System;
using System.Buffers;
using System.Diagnostics;
using IT.Multipart.Internal;

namespace IT.Multipart;

public struct MultipartSequenceReader
{
    private const byte CR = (byte)'\r';
    private const byte LF = (byte)'\n';
    private static readonly byte[] CRLF = [CR, LF];

    private readonly MultipartBoundary _boundary;
    private readonly ReadOnlySequence<byte> _sequence;
    private SequencePosition _position;

    public readonly ReadOnlySequence<byte> Sequence => _sequence;

    public readonly MultipartBoundary Boundary => _boundary;

    public readonly SequencePosition Position => _position;

    public MultipartSequenceReader(MultipartBoundary boundary, ReadOnlySequence<byte> sequence)
    {
        _boundary = boundary;
        _sequence = sequence;
        _position = sequence.Start;
    }

    public void Reset()
    {
        _position = _sequence.Start;
    }

    public bool TryReadNextSection(out MultipartSequenceSection section)
    {
        var sequence = _sequence;
#if DEBUG
        System.Text.Encoding.UTF8.TryGetString(sequence, out var utf8);
#endif
        var position = _position;
        var boundary = _boundary.SpanWithPrefix;
        SequencePosition start = default;
        if (sequence.Start.Equals(position))
        {
            Debug.Assert(boundary.Length > 3);
            start = sequence.PositionOfEnd(boundary.Slice(2));
            if (start.IsNegative()) goto invalid;
            //TODO: заменить на sequence.StartsWith(CRLF, start)
            if (!sequence.Slice(start, 2).SequenceEqual(CRLF)) goto invalid;
            start = sequence.GetPosition(2, start);
            sequence = sequence.Slice(start);
#if DEBUG
            System.Text.Encoding.UTF8.TryGetString(sequence, out utf8);
#endif
        }
        else
        {
            sequence = sequence.Slice(position);
#if DEBUG
            System.Text.Encoding.UTF8.TryGetString(sequence, out utf8);
#endif
        }

        var bodyEnd = sequence.PositionOf(boundary);
        if (bodyEnd.IsNegative()) goto invalid;
        //        var end = bodyEnd + boundaryLength + 2;
        //        if (end > sequence.Length) goto invalid;
#if DEBUG
        System.Text.Encoding.UTF8.TryGetString(sequence.Slice(sequence.Start, bodyEnd), out utf8);
#endif
        //        if (!IsEndBoundary(span[end - 2], span[end - 1])) goto invalid;
        //        bodyEnd -= 2;
        //        if (span[bodyEnd] != CR || span[bodyEnd + 1] != LF) goto invalid;
        //        span = span.Slice(0, bodyEnd);
        //#if DEBUG
        //        utf8 = System.Text.Encoding.UTF8.GetString(span);
        //#endif
        //        var index = sequence.IndexOf(CRLFCRLF);
        //        if (index < 0) throw MultipartReader.SeparatorNotFound();
        //        start += offset;
        //        index += start;
        //        section = new MultipartSection
        //        {
        //            Headers = new(start, index),
        //            Body = new(index + 4, bodyEnd + start)
        //        };
        //#if DEBUG
        //        var headersUtf8 = System.Text.Encoding.UTF8.GetString(_span[section.Headers]);
        //        var bodyUtf8 = System.Text.Encoding.UTF8.GetString(_span[section.Body]);
        //#endif
        //        _offset = end + start;
        section = default;
        return true;

    invalid:
        section = default;
        _position = _sequence.End;
        return false;
    }
}