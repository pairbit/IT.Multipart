using IT.Multipart.Internal;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Multipart;

public struct MultipartSequenceReader
{
    private const byte Dash = (byte)'-';
    private const byte CR = (byte)'\r';
    private const byte LF = (byte)'\n';
    private static readonly byte[] CRLF = [CR, LF];
    private static readonly byte[] End = [Dash, Dash];

    private readonly ReadOnlySequence<byte> _sequence;
    private readonly MultipartBoundary _boundary;
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

    public bool TryReadNextSection(out MultipartSequenceSection section, bool isStrict = true)
    {
        var sequence = _sequence;
        var position = _position;
        if (position.Equals(sequence.End))
        {
            section = default;
            return false;
        }
        var boundary = _boundary.Span;
        if (position.Equals(sequence.Start))
        {
            Debug.Assert(boundary.Length > 2);
            if (isStrict)
            {
                if (!sequence.StartsWith(boundary.Slice(2), ref position)) goto invalid;
            }
            else
            {
                position = sequence.PositionOfEnd(boundary.Slice(2));
                if (position.IsNegative()) goto invalid;
            }
            if (!sequence.StartsWith(CRLF, ref position)) goto invalid;
        }
#if DEBUG
        System.Text.Encoding.UTF8.TryGetString(sequence.Slice(position), out var utf8);
#endif
        var end = position;
        var bodyEnd = sequence.PositionOf(boundary, ref end);
        if (bodyEnd.IsNegative()) goto invalid;
        if (!sequence.StartsWith(CRLF, ref end))
        {
            if (!sequence.StartsWith(End, ref end))
                goto invalid;

            if (isStrict)
            {
                if (!sequence.StartsWith(CRLF, ref end)) goto invalid;
                if (sequence.TryGet(ref end, out var mem) && !mem.IsEmpty) goto invalid;
            }
            end = _sequence.End;
        }
        sequence = sequence.Slice(position, bodyEnd);
#if DEBUG
        System.Text.Encoding.UTF8.TryGetString(sequence, out utf8);
#endif
        var separator = sequence.PositionOf(MultipartReader.CRLFCRLF);
        if (separator.IsNegative()) throw MultipartReader.SeparatorNotFound();
        section = new MultipartSequenceSection(sequence, separator);
#if DEBUG
        System.Text.Encoding.UTF8.TryGetString(section.Headers, out var headersUtf8);
        System.Text.Encoding.UTF8.TryGetString(section.Body, out var bodyUtf8);
#endif
        _position = end;
        return true;

    invalid:
        section = default;
        _position = _sequence.End;
        return false;
    }
}