#if NET8_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace IT.Multipart;

public static class MimeEncoding
{
    // Attempt to decode MIME encoded strings.
    public static bool TryDecode(string? input,
#if NET || NETSTANDARD2_1_OR_GREATER
        [NotNullWhen(true)] 
#endif
    out string? output)
    {
        Debug.Assert(input != null);

        output = null;
        string? processedInput = input;
        ReadOnlySpan<char> processedInputSpan = processedInput.AsSpan();
        // Require quotes, min of "=?e?b??="
        if (!IsQuoted(processedInputSpan) || processedInputSpan.Length < 10)
        {
            return false;
        }

        Span<Range> parts = stackalloc Range[6];

        // "=, encodingName, encodingType, encodedData, ="
        if (processedInputSpan.Split(parts, '?') != 5 ||
            processedInputSpan[parts[0]] is not "\"=" ||
            processedInputSpan[parts[4]] is not "=\"" ||
            !processedInputSpan[parts[2]].Equals("b", StringComparison.OrdinalIgnoreCase))
        {
            // Not encoded.
            // This does not support multi-line encoding.
            // Only base64 encoding is supported, not quoted printable.
            return false;
        }

        try
        {
            Encoding encoding = Encoding.GetEncoding(processedInput[parts[1]]);
            byte[] bytes = Convert.FromBase64String(processedInput[parts[3]]);
            output = encoding.GetString(bytes, 0, bytes.Length);
            return true;
        }
        catch (ArgumentException)
        {
            // Unknown encoding or bad characters.
        }
        catch (FormatException)
        {
            // Bad base64 decoding.
        }
        return false;
    }

    // Returns true if the value starts and ends with a quote.
    private static bool IsQuoted(ReadOnlySpan<char> value)
    {
        return
            value.Length > 1 &&
            value[0] == '"' &&
            value[value.Length - 1] == '"';
    }
}
#endif