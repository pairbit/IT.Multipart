using System;
using System.Collections.Generic;

namespace IT.Multipart.Internal;

internal static class xReadOnlySpan
{
    //UTF-8
    public static bool IsUtf8(this ReadOnlySpan<byte> span)
    {
        if (span.Length != 5 || span[4] != '8' || span[3] != '-') return false;

        var by = span[0];
        if (by != 'u' && by != 'U') return false;

        by = span[1];
        if (by != 't' && by != 'T') return false;

        by = span[2];
        if (by != 'f' && by != 'F') return false;

        return true;
    }

    public static int IndexOfPart<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value, out int length)
        where T : IEquatable<T>
#if NET7_0_OR_GREATER
        ?
#endif
    {
        if (span.Length == 0) throw new ArgumentException("span is empty", nameof(span));

        var maxLength = value.Length;
        if (maxLength == 0) throw new ArgumentException("value is empty", nameof(value));

        var v = value[0];
        var index = span.IndexOf(v);
        if (index == -1)
        {
            length = 0;
            return -1;
        }

        if (maxLength == 1)
        {
            length = 1;
            return index;
        }

        var len = 1;

        v = value[1];

        for (int i = index + 1; i < span.Length; i++)
        {
            var s = span[i];
            if (EqualityComparer<T>.Default.Equals(v, s))
            {
                if (++len == maxLength)
                {
                    length = len;
                    return index;
                }
                v = value[len];
            }
            else if (len > 0)
            {
                v = value[0];
                if (EqualityComparer<T>.Default.Equals(v, s))
                {
                    index = i;
                }
                else
                {
                    i++;
                    index = span.Slice(i).IndexOf(v);
                    if (index == -1)
                    {
                        length = 0;
                        return -1;
                    }
                    i = index = index + i;
                }
                len = 1;
                v = value[1];
            }
        }
        length = len;
        return index;
    }

#if NETSTANDARD2_0_OR_GREATER

    internal static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
    {
        if (span.Length != other.Length) return false;

        if (typeof(T).IsValueType)
        {
            if (comparer is null || comparer == EqualityComparer<T>.Default)
            {
                // Otherwise, compare each element using EqualityComparer<T>.Default.Equals in a way that will enable it to devirtualize.
                for (int i = 0; i < span.Length; i++)
                {
                    if (!EqualityComparer<T>.Default.Equals(span[i], other[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // Use the comparer to compare each element.
        comparer ??= EqualityComparer<T>.Default;
        for (int i = 0; i < span.Length; i++)
        {
            if (!comparer.Equals(span[i], other[i]))
            {
                return false;
            }
        }

        return true;
    }
#endif
}