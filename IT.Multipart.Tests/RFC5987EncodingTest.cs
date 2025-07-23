using System.Text;
using IT.Multipart.Internal;

namespace IT.Multipart.Tests;

internal class RFC5987EncodingTest
{
    [Test]
    public void IsUtf8Test()
    {
        Assert.That("utf-8"u8.IsUtf8(), Is.True);
        Assert.That("UTF-8"u8.IsUtf8(), Is.True);
        Assert.That("UTf-8"u8.IsUtf8(), Is.True);
        Assert.That("Utf-8"u8.IsUtf8(), Is.True);
        Assert.That("UtF-8"u8.IsUtf8(), Is.True);
        Assert.That("uTF-8"u8.IsUtf8(), Is.True);
        Assert.That("uTf-8"u8.IsUtf8(), Is.True);
        Assert.That("utF-8"u8.IsUtf8(), Is.True);

        Assert.That("utf-88"u8.IsUtf8(), Is.False);
        Assert.That("utf-7"u8.IsUtf8(), Is.False);
        Assert.That("utf+7"u8.IsUtf8(), Is.False);
        Assert.That("atf-8"u8.IsUtf8(), Is.False);
        Assert.That("ubf-8"u8.IsUtf8(), Is.False);
        Assert.That("utc-8"u8.IsUtf8(), Is.False);
    }

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
        TryDecodeUtf8InPlaceTest("%d0%b8%d0%bc%d1%8f.pdf"u8, "имя.pdf"u8);
        TryDecodeUtf8InPlaceTest("myname"u8, "myname"u8);
        TryDecodeUtf8InPlaceTest("%25D0"u8, "%D0"u8);
        TryDecodeUtf8InPlaceTest("%e2%82%ac%20exchange%20rates"u8, "€ exchange rates"u8);
        //TryDecodeUtf8InPlaceTest("%A3%20rates"u8, "£ rates"u8);
    }

    //[Test]
    public void TryDecodeInPlaceTest()
    {
        TryDecodeInPlaceTest(Encoding.GetEncoding("iso-8859-1"), "%A3%20rates"u8, "£ rates"u8);
    }

    [Test]
    public void TryDecodeTest()
    {
        Assert.That(RFC5987Encoding.TryDecode("utf-8''%e2%82%ac%20exchange%20rates", out var rfc5987Decoded), Is.True);
        Assert.That(rfc5987Decoded, Is.EqualTo("€ exchange rates"));

        Assert.That(RFC5987Encoding.TryDecode("UTF-8''%c2%a3%20and%20%e2%82%ac%20rates", out rfc5987Decoded), Is.True);
        Assert.That(rfc5987Decoded, Is.EqualTo("£ and € rates"));

        Assert.That(RFC5987Encoding.TryDecode("iso-8859-1'en'%A3%20rates", out rfc5987Decoded), Is.True);
        Assert.That(rfc5987Decoded, Is.EqualTo("£ rates"));
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

        if (!buffer.AsSpan(0, written).SequenceEqual(decoded))
            Assert.Fail(Encoding.UTF8.GetString(buffer.AsSpan(0, written)));
    }

    private static void TryDecodeInPlaceTest(Encoding encoding, ReadOnlySpan<byte> encoded, ReadOnlySpan<byte> decoded)
    {
        var buffer = encoded.ToArray();
        Assert.That(RFC5987Encoding.TryDecodeInPlace(encoding, buffer, out var written), Is.True);

        if (!buffer.AsSpan(0, written).SequenceEqual(decoded))
            Assert.Fail(encoding.GetString(buffer.AsSpan(0, written)));
    }
}