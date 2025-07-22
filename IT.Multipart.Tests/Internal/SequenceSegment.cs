using System.Buffers;
using System.Diagnostics;

namespace IT.Multipart.Tests;

public class SequenceSegment<T> : ReadOnlySequenceSegment<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new ReadOnlyMemory<T> Memory
    {
        get => base.Memory;
        set => base.Memory = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new SequenceSegment<T>? Next
    {
        get => (SequenceSegment<T>?)base.Next;
        set => base.Next = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new long RunningIndex
    {
        get => base.RunningIndex;
        set => base.RunningIndex = value;
    }

    public SequenceSegment<T> Append(ReadOnlyMemory<T> memory)
    {
        var next = new SequenceSegment<T>
        {
            Memory = memory,
            RunningIndex = RunningIndex + Memory.Length
        };

        Next = next;

        return next;
    }
}