namespace IT.Multipart.Tests;

internal class MultipartHeadersReaderTest
{
    [Test]
    public void ReadNextHeaderTest()
    {
        var span = "Content-Disposition: form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\"\r\nContent-Type: text/xml"u8;
        var reader = new MultipartHeadersReader(span);

        Assert.That(reader.ReadNextHeader(out var header, trimValue: default), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("Content-Disposition"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual(" form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\""u8), Is.True);

        Assert.That(reader.ReadNextHeader(out header, trimValue: default), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("Content-Type"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual(" text/xml"u8), Is.True);

        Assert.That(reader.ReadNextHeader(out header), Is.EqualTo(MultipartReadingStatus.End));
        Assert.That(header, Is.EqualTo(default(MultipartHeader)));

        reader.Reset();

        Assert.That(reader.ReadNextHeader(out header), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("Content-Disposition"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\""u8), Is.True);

        Assert.That(reader.ReadNextHeader(out header), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("Content-Type"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("text/xml"u8), Is.True);

        reader.Reset();

        Assert.That(reader.ReadNextContentDisposition(out var value), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\""u8), Is.True);

        Assert.That(reader.ReadNextContentType(out value), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("text/xml"u8), Is.True);

        span = "Content-Type:text/xml"u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.ReadNextHeader(out header, trimValue: default), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("Content-Type"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("text/xml"u8), Is.True);

        reader.Reset();
        Assert.That(reader.ReadNextHeader(out header), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("Content-Type"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("text/xml"u8), Is.True);

        span = " Content-Disposition : \n\r\t\v\f form-data; name=transform; filename=\"Transform - utf8.xsl\" \n\r\t\v\f \r\n Content-Type : \n\r\t\v\f text/xml \n\r\t\v\f "u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.ReadNextHeader(out header, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual(" Content-Disposition "u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("form-data; name=transform; filename=\"Transform - utf8.xsl\""u8), Is.True);

        Assert.That(reader.ReadNextHeader(out header, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual(" Content-Type "u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("text/xml"u8), Is.True);

        span = "empty:\r\nempty2:"u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.ReadNextHeader(out header), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("empty"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);

        Assert.That(reader.ReadNextHeader(out header), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("empty2"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);

        span = "empty: \n\r\t\v\f \r\nempty2: \n\r\t\v\f "u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.ReadNextHeader(out header, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("empty"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);

        Assert.That(reader.ReadNextHeader(out header, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[header.Name].SequenceEqual("empty2"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);
    }

    [Test]
    public void ReadNextHeaderInvalidTest()
    {
        ReadNextHeaderInvalid("form-data"u8, MultipartReadingStatus.HeaderSeparatorNotFound);
        ReadNextHeaderInvalid(": form-data"u8, MultipartReadingStatus.HeaderNameNotFound);
    }

    /*
    [Test]
    public void TryReadNextHeaderTest()
    {
        var span = "Content-Disposition: form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\"\r\nContent-Type: text/xml"u8;
        var reader = new MultipartHeadersReader(span);

        Assert.That(reader.TryReadNextHeader(out var header), Is.True);
        Assert.That(span[header.Name].SequenceEqual("Content-Disposition"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\""u8), Is.True);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual("Content-Type"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("text/xml"u8), Is.True);

        Assert.That(reader.TryReadNextHeader(out header), Is.False);
        Assert.That(header, Is.EqualTo(default(MultipartHeader)));

        reader.Reset();
        Assert.That(reader.TryReadNextContentDisposition(out var value), Is.True);
        Assert.That(span[value].SequenceEqual("form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\""u8), Is.True);

        Assert.That(reader.TryReadNextContentType(out value), Is.True);
        Assert.That(span[value].SequenceEqual("text/xml"u8), Is.True);

        span = "Content-Type:text/xml"u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual("Content-Type"u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("text/xml"u8), Is.True);

        span = " Content-Disposition : \n\r\t\v\f form-data; name=transform; filename=\"Transform - utf8.xsl\" \n\r\t\v\f \r\n Content-Type : \n\r\t\v\f text/xml \n\r\t\v\f "u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual(" Content-Disposition "u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("form-data; name=transform; filename=\"Transform - utf8.xsl\""u8), Is.True);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual(" Content-Type "u8), Is.True);
        Assert.That(span[header.Value].SequenceEqual("text/xml"u8), Is.True);

        span = "empty:\r\nempty2:"u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual("empty"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual("empty2"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);

        span = "empty: \n\r\t\v\f \r\nempty2: \n\r\t\v\f "u8;
        reader = new MultipartHeadersReader(span);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual("empty"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextHeader(out header), Is.True);
        Assert.That(span[header.Name].SequenceEqual("empty2"u8), Is.True);
        Assert.That(span[header.Value].IsEmpty, Is.True);

        try
        {
            new MultipartHeadersReader("form-data"u8).TryReadNextHeader(out _);
            Assert.Fail();
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Is.EqualTo(MultipartHeadersReader.SeparatorNotFound().Message));
        }

        try
        {
            new MultipartHeadersReader(": form-data"u8).TryReadNextHeader(out _);
            Assert.Fail();
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Is.EqualTo(MultipartHeadersReader.NameNotFound().Message));
        }
    }
    */

    private static void ReadNextHeaderInvalid(ReadOnlySpan<byte> span, MultipartReadingStatus invalidStatus, TrimOptions trimValue = default)
    {
        var offset = (int)invalidStatus;
        Assert.That(offset, Is.LessThan(0));

        var reader = new MultipartHeadersReader(span);
        Assert.That(reader.ReadNextHeader(out var section, trimValue), Is.EqualTo(invalidStatus));
        Assert.That(section, Is.EqualTo(default(MultipartHeader)));

        Assert.That(reader.Offset, Is.EqualTo(offset));

        Assert.That(reader.ReadNextHeader(out section, trimValue), Is.EqualTo(invalidStatus));
        Assert.That(section, Is.EqualTo(default(MultipartHeader)));
    }
}