using System;
using System.Text;

namespace IT.Multipart;

//System.Net.Http.Headers.ContentDispositionHeaderValue
//RFC 5987 encoding
//charset "'" [ language ] "'" value-chars
//title*=iso-8859-1'en'%A3%20rates
//title*=UTF-8''%c2%a3%20and%20%e2%82%ac%20rates
//title*=utf-8''%e2%82%ac%20exchange%20rates
public static class RFC5987Encoding
{
    private const char QuoteChar = '\'';
    private const byte QuoteByte = (byte)QuoteChar;
    private const byte Percent = (byte)'%';

    private static readonly byte[] utf8 = "utf-8"u8.ToArray();
    private static readonly byte[] UTF8 = "UTF-8"u8.ToArray();

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

    public static bool TryParse(ReadOnlySpan<byte> bytes, out RFC5987Value value)
    {
        var firstIndex = bytes.IndexOf(QuoteByte);
        if (firstIndex == -1)
        {
            value = default;
            return false;
        }

        int secondIndex = bytes.Slice(firstIndex + 1).IndexOf(QuoteByte);
        if (secondIndex == -1)
        {
            value = default;
            return false;
        }

        value = new RFC5987Value(firstIndex, secondIndex + firstIndex + 1);
        return true;
    }

    public static bool TryDecodeInPlace(ReadOnlySpan<byte> encodingBytes, Span<byte> encoded, out int written)
    {
        if (encodingBytes.SequenceEqual(utf8) || encodingBytes.SequenceEqual(UTF8))
            return TryDecodeUtf8InPlace(encoded, out written);

        //Encoding.GetEncoding()

        throw new NotImplementedException();
    }

    public static bool TryDecodeUtf8InPlace(Span<byte> encoded, out int written)
    {
#if DEBUG
        var encodedUtf8 = Encoding.UTF8.GetString(encoded);
#endif
        written = 0;
        for (int i = 0; i < encoded.Length; i++)
        {
            var by = encoded[i];
            if (by == Percent) // %FF
            {
                if (!TryDecodeHex(encoded[i + 1], encoded[i + 2], out var utf8))
                    return false;

                encoded[written++] = utf8;
                i += 2;
            }
            else
            {
                encoded[written++] = encoded[i];
            }
        }
        return true;
    }

    private static bool TryDecodeHex(byte first, byte second, out byte utf8)
    {
        int a = HexLookup[first];
        int b = HexLookup[second];

        if ((a | b) == 0xFF)
        {
            // either a or b is 0xFF (invalid)
            utf8 = 0xFF;
            return false;
        }

        utf8 = (byte)((a << 4) | b);
        return true;
    }

    /// <summary>Map from an ASCII char to its hex value, e.g. arr['b'] == 11. 0xFF means it's not a hex digit.</summary>
    private readonly static byte[] HexLookup =
    [
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
        0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 63
        0xFF, 0xA,  0xB,  0xC,  0xD,  0xE,  0xF,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 79
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 95
        0xFF, 0xa,  0xb,  0xc,  0xd,  0xe,  0xf,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 111
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 127
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 143
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 159
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 175
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 191
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 207
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 223
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 239
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // 255
    ];
}