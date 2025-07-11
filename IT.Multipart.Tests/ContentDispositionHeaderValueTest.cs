using System;
using System.Net.Http.Headers;
using System.Text;

namespace IT.Multipart.Tests;

internal class ContentDispositionHeaderValueTest
{
    [Test]
    public void Test()
    {
        var fileName = "name имя.pdf";

        var header = new ContentDispositionHeaderValue("form-data");
        header.Name = "name";
        header.FileName = fileName;
        header.FileNameStar = fileName;

        var str = header.ToString();
        Assert.That(str, Is.EqualTo("form-data; name=name; filename=\"=?utf-8?B?bmFtZSDQuNC80Y8ucGRm?=\"; filename*=utf-8''name%20%D0%B8%D0%BC%D1%8F.pdf"));

        header = ContentDispositionHeaderValue.Parse(str);
        Assert.That(header.FileName, Is.EqualTo(fileName));
        Assert.That(header.FileNameStar, Is.EqualTo(fileName));

        //Assert.That(RFC5987Encoding.TryDecode("iso-8859-1'en'%A3%20rates", out var rfc5987Decoded), Is.True);
        //Assert.That(rfc5987Decoded, Is.EqualTo(fileName));

        Assert.That(RFC5987Encoding.TryDecode("utf-8'ru'name%20%D0%B8%D0%BC%D1%8F.pdf", out var rfc5987Decoded), Is.True);
        Assert.That(rfc5987Decoded, Is.EqualTo(fileName));

        var buffer = "utf-8'ru'name%20%D0%B8%D0%BC%D1%8F.pdf"u8.ToArray();

        Assert.That(RFC5987Encoding.TryParse(buffer, out var rfc5987), Is.True);

        var encoded = buffer.AsSpan(rfc5987.EncodedStart);
        Assert.That(RFC5987Encoding.TryDecodeInPlace(
            buffer.AsSpan(0, rfc5987.CharsetEnd),
            encoded, out var written), Is.True);

        Assert.That(Encoding.UTF8.GetString(encoded.Slice(0, written)), Is.EqualTo(fileName));

#if NET8_0_OR_GREATER
        Assert.That(MimeEncoding.TryDecode("\"=?utf-8?B?bmFtZSDQuNC80Y8ucGRm?=\"", out var mimeDecoded), Is.True);
        Assert.That(mimeDecoded, Is.EqualTo(fileName));
#endif
    }
    
    //[Test]
    public void tette()
    {
        var fileName = "%D0";

        var header = new ContentDispositionHeaderValue("form-data");
        header.Name = "name";
        header.FileName = fileName;
        header.FileNameStar = fileName;

        var str = header.ToString();

    }
}