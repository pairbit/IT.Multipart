using System.Buffers;
using System.Text;

namespace IT.Multipart.Internal;

internal static class xEncoding
{
    private const int MaxLength = 2147483591;

    public static bool TryGetString(this Encoding encoding, ReadOnlySequence<byte> sequence, out string? str)
    {
        var length = sequence.Length;
        if (length == 0)
        {
            str = string.Empty;
            return true;
        }

        if (length <= MaxLength)
        {
            //TODO: rent buffer
            str = encoding.GetString(sequence.ToArray());
            return true;
        }

        str = null;
        return false;
    }
}