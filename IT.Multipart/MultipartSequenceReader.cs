using System;
using System.Buffers;

namespace IT.Multipart;

public struct MultipartSequenceReader
{
    private readonly MultipartBoundary _boundary;
    private readonly ReadOnlySequence<byte> _sequence;
    private SequencePosition _position;

    public readonly ReadOnlySequence<byte> Sequence => _sequence;

    public readonly MultipartBoundary Boundary => _boundary;

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
        throw new NotImplementedException();
    }
}