namespace IT.Multipart.Tests;

internal class MultipartReaderTest
{
    [Test]
    public void ReadNextSectionNoStrictTest()
    {
        var span = "[[[[------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"\r\n"u8 +
"<data>mydata</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=\"name\"\r\n"u8 +
"\r\n"u8 +
"package name\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9--]]]]"u8;
        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.ReadNextSection(out var section, isStrict: false), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\nContent-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>mydata</data>"u8), Is.True);

        Assert.That(reader.ReadNextSection(out section, isStrict: false), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=\"name\""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);

        Assert.That(reader.ReadNextSection(out section, isStrict: false), Is.EqualTo(MultipartReadingStatus.End));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        span = "[[[------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--]]]"u8;
        reader = new MultipartReader(boundary, span);
        Assert.That(reader.ReadNextSection(out section, isStrict: false), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[section.Headers].IsEmpty, Is.True);
        Assert.That(span[section.Body].IsEmpty, Is.True);

        Assert.That(reader.ReadNextSection(out _), Is.EqualTo(MultipartReadingStatus.End));
    }

    [Test]
    public void ReadNextSectionTest()
    {
        var span = "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"\r\n"u8 +
"<data>mydata</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=\"name\"\r\n"u8 +
"\r\n"u8 +
"package name\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8;
        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.ReadNextSection(out var section), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\nContent-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>mydata</data>"u8), Is.True);

        Assert.That(reader.ReadNextSection(out section), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=\"name\""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);

        Assert.That(reader.ReadNextSection(out section), Is.EqualTo(MultipartReadingStatus.End));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        span = "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8;
        reader = new MultipartReader(boundary, span);
        Assert.That(reader.ReadNextSection(out section), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[section.Headers].IsEmpty, Is.True);
        Assert.That(span[section.Body].IsEmpty, Is.True);

        Assert.That(reader.ReadNextSection(out _), Is.EqualTo(MultipartReadingStatus.End));
    }

    [Test]
    public void ReadNextSectionInvalid()
    {
        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;

        Assert.That(new MultipartReader(boundary,
            "------WebKit"u8).ReadNextSection(out var section), 
            Is.EqualTo(MultipartReadingStatus.StartBoundaryNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9"u8)
            .ReadNextSection(out section), 
            Is.EqualTo(MultipartReadingStatus.StartBoundaryCRLFNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r  "u8)
            .ReadNextSection(out section), 
            Is.EqualTo(MultipartReadingStatus.StartBoundaryCRLFNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n "u8)
            .ReadNextSection(out section), 
            Is.EqualTo(MultipartReadingStatus.BoundaryNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8)
            .ReadNextSection(out section), 
            Is.EqualTo(MultipartReadingStatus.BoundaryNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9"u8)
            .ReadNextSection(out section),
            Is.EqualTo(MultipartReadingStatus.BoundaryNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9  "u8)
            .ReadNextSection(out section),
            Is.EqualTo(MultipartReadingStatus.BoundaryNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8)
            .ReadNextSection(out section), 
            Is.EqualTo(MultipartReadingStatus.BoundaryNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9--"u8)
            .ReadNextSection(out section), 
            Is.EqualTo(MultipartReadingStatus.BoundaryNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8)
            .ReadNextSection(out section),
            Is.EqualTo(MultipartReadingStatus.SectionSeparatorNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8)
            .ReadNextSection(out section),
            Is.EqualTo(MultipartReadingStatus.SectionSeparatorNotFound));
        Assert.That(section, Is.EqualTo(default(MultipartSection)));
    }

    [Test]
    public void TryReadNextSectionNoStrictTest()
    {
        var span = "[[[[------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"\r\n"u8 +
"<data>mydata</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=\"name\"\r\n"u8 +
"\r\n"u8 +
"package name\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9--]]]]"u8;
        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.TryReadNextSection(out var section, isStrict: false), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\nContent-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>mydata</data>"u8), Is.True);

        Assert.That(reader.TryReadNextSection(out section, isStrict: false), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=\"name\""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);

        Assert.That(reader.TryReadNextSection(out section, isStrict: false), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        span = "[[[------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--]]]"u8;
        reader = new MultipartReader(boundary, span);
        Assert.That(reader.TryReadNextSection(out section, isStrict: false), Is.True);
        Assert.That(span[section.Headers].IsEmpty, Is.True);
        Assert.That(span[section.Body].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextSection(out _), Is.False);
    }

    [Test]
    public void TryReadNextSectionTest()
    {
        var span = "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"\r\n"u8 +
"<data>mydata</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=\"name\"\r\n"u8 +
"\r\n"u8 +
"package name\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8;
        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.TryReadNextSection(out var section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\nContent-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>mydata</data>"u8), Is.True);

        Assert.That(reader.TryReadNextSection(out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=\"name\""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);

        Assert.That(reader.TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        span = "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8;
        reader = new MultipartReader(boundary, span);
        Assert.That(reader.TryReadNextSection(out section), Is.True);
        Assert.That(span[section.Headers].IsEmpty, Is.True);
        Assert.That(span[section.Body].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextSection(out _), Is.False);
    }

    [Test]
    public void TryReadNextSectionByContentDispositionTest()
    {
        var span = "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: attachment; name=data; filename=\"Transform;utf8.xsl\"\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"\r\n"u8 +
"<data>data attachment</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=\"data\"; filename=\"Transform;utf8.xsl\"\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"\r\n"u8 +
"<data>data form-data</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=\"name\"\r\n"u8 +
"\r\n"u8 +
"package name\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8;

        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.TryReadNextSectionByContentDisposition("attachment"u8, "data"u8, out var section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: attachment; name=data; filename=\"Transform;utf8.xsl\"\r\nContent-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>data attachment</data>"u8), Is.True);

        Assert.That(reader.TryReadNextSectionByContentDispositionFormData("data"u8, out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=\"data\"; filename=\"Transform;utf8.xsl\"\r\nContent-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>data form-data</data>"u8), Is.True);

        Assert.That(reader.TryReadNextSectionByContentDispositionFormData("name"u8, out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=\"name\""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);
    }

    [Test]
    public void TryFindSectionByContentDispositionTest()
    {
        var span = "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"Content-Disposition: attachment; filename=\"Transform;utf8.xsl\"; name=data\r\n"u8 +
"\r\n"u8 +
"<data>data attachment</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Type: text/xml\r\n"u8 +
"Content-Disposition: form-data; filename=\"Transform;utf8.xsl\"; name=\"data\"\r\n"u8 +
"\r\n"u8 +
"<data>data form-data</data>\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8 +
"Content-Disposition: form-data; name=\"name\"\r\n"u8 +
"\r\n"u8 +
"package name\r\n"u8 +
"------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8;

        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.TryFindSectionByContentDispositionFormData("data"u8, out var section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Type: text/xml\r\nContent-Disposition: form-data; filename=\"Transform;utf8.xsl\"; name=\"data\""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>data form-data</data>"u8), Is.True);

        reader.Reset();

        Assert.That(reader.TryFindSectionByContentDisposition("attachment"u8, "data"u8, out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Type: text/xml\r\nContent-Disposition: attachment; filename=\"Transform;utf8.xsl\"; name=data"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>data attachment</data>"u8), Is.True);

        Assert.That(reader.TryFindSectionByContentDispositionFormData("name"u8, out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual("Content-Disposition: form-data; name=\"name\""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);
    }

    [Test]
    public void TryReadNextSectionInvalid()
    {
        var boundary = "\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8;

        Assert.That(new MultipartReader(boundary,
            "------WebKit"u8)
            .TryReadNextSection(out var section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9"u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r  "u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n "u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9"u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9  "u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        Assert.That(new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n  ------WebKitFormBoundarylng3rD4syfIK3fT9--"u8)
            .TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        try
        {
            new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9\r\n"u8)
            .TryReadNextSection(out section);
            Assert.Fail();
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Is.EqualTo(MultipartReader.SeparatorNotFound().Message));
        }

        try
        {
            new MultipartReader(boundary,
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8)
            .TryReadNextSection(out section);
            Assert.Fail();
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Is.EqualTo(MultipartReader.SeparatorNotFound().Message));
        }

        var reader = new MultipartReader(boundary, "------WebKit"u8);
        Assert.That(reader.TryReadNextSection(out _), Is.False);
        Assert.That(reader.TryReadNextSection(out _), Is.False);
    }
}