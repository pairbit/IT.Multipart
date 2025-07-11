using System.Buffers.Text;

namespace IT.Multipart.Tests;

internal class RFC5987EncodingTest
{
    [Test]
    public void TryParseTest()
    {
        TryParseTest("1''2"u8, "1"u8, default, "2"u8);
        TryParseTest("1'2'3"u8, "1"u8, "2"u8, "3"u8);
        TryParseTest("utf-8'ru'name%20%D0%B8%D0%BC%D1%8F.pdf"u8,
            "utf-8"u8, "ru"u8, "name%20%D0%B8%D0%BC%D1%8F.pdf"u8);

        TryParseTest("1'2'''3"u8, "1"u8, "2"u8, "''3"u8);
        TryParseTest("1''"u8, "1"u8, default, default);

        Assert.That(RFC5987Encoding.TryParse("1"u8, out _), Is.False);
        Assert.That(RFC5987Encoding.TryParse("'"u8, out _), Is.False);
        Assert.That(RFC5987Encoding.TryParse("a'"u8, out _), Is.False);
    }
    
    [Test]
    public void TryDecodeUtf8InPlaceTest()
    {
        TryDecodeUtf8InPlaceTest("name%20%D0%B8%D0%BC%D1%8F.pdf"u8, "name имя.pdf"u8);
        
    }

    private static void TryParseTest(ReadOnlySpan<byte> bytes,
        ReadOnlySpan<byte> charset, ReadOnlySpan<byte> language,
        ReadOnlySpan<byte> encoded)
    {
        Assert.That(RFC5987Encoding.TryParse(bytes, out var rfc5987), Is.True);
        Assert.That(bytes.Slice(0, rfc5987.CharsetEnd).SequenceEqual(charset), Is.True);
        Assert.That(bytes.Slice(rfc5987.LanguageStart, rfc5987.LanguageLength).SequenceEqual(language), Is.True);
        Assert.That(bytes[rfc5987.Language].SequenceEqual(language), Is.True);
        Assert.That(bytes.Slice(rfc5987.EncodedStart).SequenceEqual(encoded), Is.True);
    }

    private static void TryDecodeUtf8InPlaceTest(ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> decoded)
    {
        var buffer = encoded.ToArray();
        Assert.That(RFC5987Encoding.TryDecodeUtf8InPlace(buffer, out var written), Is.True);
        Assert.That(buffer.AsSpan(0, written).SequenceEqual(decoded), Is.True);
    }
}