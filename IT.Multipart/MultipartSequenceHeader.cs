using System.Buffers;

namespace IT.Multipart;

public readonly struct MultipartSequenceHeader
{
    public ReadOnlySequence<byte> Name { get; init; }

    public ReadOnlySequence<byte> Value { get; init; }
}