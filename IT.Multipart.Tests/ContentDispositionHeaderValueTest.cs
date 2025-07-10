using System.Net.Http.Headers;

namespace IT.Multipart.Tests;

internal class ContentDispositionHeaderValueTest
{
    [Test]
    public void Test()
    {
        var fileName = "имя файла.pdf";

        var header = new ContentDispositionHeaderValue("form-data");
        header.Name = "name";
        header.FileName = fileName;
        header.FileNameStar = fileName;

        var str = header.ToString();
        Assert.That(str, Is.EqualTo("form-data; name=name; filename=\"=?utf-8?B?0LjQvNGPINGE0LDQudC70LAucGRm?=\"; filename*=utf-8''%D0%B8%D0%BC%D1%8F%20%D1%84%D0%B0%D0%B9%D0%BB%D0%B0.pdf"));

        header = ContentDispositionHeaderValue.Parse(str);
        Assert.That(header.FileName, Is.EqualTo(fileName));
        Assert.That(header.FileNameStar, Is.EqualTo(fileName));

        Assert.That(RFC5987Encoding.TryDecode("utf-8''%D0%B8%D0%BC%D1%8F%20%D1%84%D0%B0%D0%B9%D0%BB%D0%B0.pdf", out var rfc5987Decoded), Is.True);
        Assert.That(rfc5987Decoded, Is.EqualTo(fileName));

#if NET8_0_OR_GREATER
        Assert.That(MimeEncoding.TryDecode("\"=?utf-8?B?0LjQvNGPINGE0LDQudC70LAucGRm?=\"", out var mimeDecoded), Is.True);
        Assert.That(mimeDecoded, Is.EqualTo(fileName));
#endif
    }
}