using System;
using System.Buffers;

namespace IT.Multipart;

public struct MultipartSequenceHeadersReader
{
    private readonly ReadOnlySequence<byte> _sequence;
    private SequencePosition _position;

    public readonly ReadOnlySequence<byte> Sequence => _sequence;

    public readonly SequencePosition Position => _position;

    public MultipartSequenceHeadersReader(ReadOnlySequence<byte> sequence)
    {
        _sequence = sequence;
        _position = sequence.Start;
    }

    public void Reset()
    {
        _position = _sequence.Start;
    }

    public bool TryReadNextHeader(out MultipartSequenceHeader header)
    {
        throw new NotImplementedException();
    }

    public bool TryReadNextHeaderValueByName(ReadOnlySpan<byte> name, out ReadOnlySequence<byte> value)
    {
        throw new NotImplementedException();
    }

    public bool TryReadNextContentDisposition(out ReadOnlySequence<byte> value)
        => TryReadNextHeaderValueByName("Content-Disposition"u8, out value);

    public bool TryReadNextContentType(out ReadOnlySequence<byte> value)
        => TryReadNextHeaderValueByName("Content-Type"u8, out value);
}