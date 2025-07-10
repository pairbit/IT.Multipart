using System;
using System.Collections.Generic;
using System.Text;

namespace IT.Multipart;

public static class RFC5987Encoding
{
    // Attempt to decode using RFC 5987 encoding.
    // encoding'language'my%20string
    public static bool TryDecode(string input, out string? output)
    {
        output = null;

        int quoteIndex = input.IndexOf('\'');
        if (quoteIndex == -1) return false;

        int lastQuoteIndex = input.LastIndexOf('\'');
        if (quoteIndex == lastQuoteIndex || input.IndexOf('\'', quoteIndex + 1) != lastQuoteIndex)
        {
            return false;
        }

        string encodingString = input.Substring(0, quoteIndex);
        string dataString = input.Substring(lastQuoteIndex + 1);

        var decoded = new StringBuilder();
        try
        {
            var encoding = Encoding.GetEncoding(encodingString);

            byte[] unescapedBytes = new byte[dataString.Length];
            int unescapedBytesCount = 0;
            for (int index = 0; index < dataString.Length; index++)
            {
                if (Uri.IsHexEncoding(dataString, index)) // %FF
                {
                    // Unescape and cache bytes, multi-byte characters must be decoded all at once.
                    unescapedBytes[unescapedBytesCount++] = (byte)Uri.HexUnescape(dataString, ref index);
                    index--; // HexUnescape did +=3; Offset the for loop's ++
                }
                else
                {
                    if (unescapedBytesCount > 0)
                    {
                        // Decode any previously cached bytes.
                        decoded.Append(encoding.GetString(unescapedBytes, 0, unescapedBytesCount));
                        unescapedBytesCount = 0;
                    }
                    decoded.Append(dataString[index]); // Normal safe character.
                }
            }

            if (unescapedBytesCount > 0)
            {
                // Decode any previously cached bytes.
                decoded.Append(encoding.GetString(unescapedBytes, 0, unescapedBytesCount));
            }
        }
        catch (ArgumentException)
        {
            return false; // Unknown encoding or bad characters.
        }

        output = decoded.ToString();
        return true;
    }
}