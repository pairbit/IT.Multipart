using System;
using System.Buffers;

namespace IT.Multipart;

public readonly struct MultipartSequenceSection
{
    private readonly ReadOnlySequence<byte> _sequence;
    private readonly SequencePosition _position;

    public ReadOnlySequence<byte> Headers => _sequence.Slice(0, _position);

    public ReadOnlySequence<byte> Body => _sequence.Slice(_sequence.GetPosition(4, _position));

    public MultipartSequenceSection(ReadOnlySequence<byte> sequence, SequencePosition position)
    {
        _sequence = sequence;
        _position = position;
    }
}