using System.Buffers;

namespace IT.Multipart.Tests;

public static class xReadOnlyMemory
{
    public static ReadOnlySequence<T> SplitBySegments<T>(this Memory<T> memory, int maxSegments)
        => SplitBySegments((ReadOnlyMemory<T>)memory, maxSegments);

    public static ReadOnlySequence<T> SplitBySegments<T>(this ReadOnlyMemory<T> memory, int maxSegments)
    {
        if (maxSegments <= 0) throw new ArgumentOutOfRangeException(nameof(maxSegments));

        var length = memory.Length;
        if (length == 0) return ReadOnlySequence<T>.Empty;

        var segments = length < maxSegments ? length : maxSegments;
        if (segments == 1) return new(memory);

        var segmentLength = length / segments;

        var start = new SequenceSegment<T>
        {
            Memory = memory[..segmentLength]
        };

        memory = memory[segmentLength..];
        var end = start;

        for (int i = segments - 2; i > 0; i--)
        {
            end = end.Append(memory[..segmentLength]);

            memory = memory[segmentLength..];
        }

        end = end.Append(memory);
        return new ReadOnlySequence<T>(start, 0, end, end.Memory.Length);
    }
}