﻿namespace IT.Multipart.Tests;

internal class MultipartReaderTest
{
    [Test]
    public void TryReadNextSectionTest()
    {
        var span = @"------WebKitFormBoundarylng3rD4syfIK3fT9
Content-Disposition: form-data; name=transform; filename=""Transform-utf8.xsl""
Content-Type: text/xml

<data>mydata</data>
------WebKitFormBoundarylng3rD4syfIK3fT9
Content-Disposition: form-data; name=""name""

package name
------WebKitFormBoundarylng3rD4syfIK3fT9--
123
"u8;
        var boundary = new MultipartBoundary("------WebKitFormBoundarylng3rD4syfIK3fT9"u8);
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.TryReadNextSection(out var section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual(@"Content-Disposition: form-data; name=transform; filename=""Transform-utf8.xsl""
Content-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>mydata</data>"u8), Is.True);

        Assert.That(reader.TryReadNextSection(out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual(@"Content-Disposition: form-data; name=""name"""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);

        Assert.That(reader.TryReadNextSection(out section), Is.False);
        Assert.That(section, Is.EqualTo(default(MultipartSection)));

        span = "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--"u8;
        reader = new MultipartReader(boundary, span);
        Assert.That(reader.TryReadNextSection(out section), Is.True);
        Assert.That(span[section.Headers].IsEmpty, Is.True);
        Assert.That(span[section.Body].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextSection(out _), Is.False);
    }

    [Test]
    public void TryReadNextSectionByContentDispositionTest()
    {
        var span = @"------WebKitFormBoundarylng3rD4syfIK3fT9
Content-Disposition: attachment; filename=""Transform-utf8.xsl""; name=data
Content-Type: text/xml

<data>data attachment</data>
------WebKitFormBoundarylng3rD4syfIK3fT9
Content-Disposition: form-data; filename=""Transform-utf8.xsl""; name=""data""
Content-Type: text/xml

<data>data form-data</data>
------WebKitFormBoundarylng3rD4syfIK3fT9
Content-Disposition: form-data; name=""name""

package name
------WebKitFormBoundarylng3rD4syfIK3fT9--
123
"u8;
        var boundary = new MultipartBoundary("------WebKitFormBoundarylng3rD4syfIK3fT9"u8);
        var reader = new MultipartReader(boundary, span);

        Assert.That(reader.TryReadNextSectionByContentDisposition("form-data"u8, "data"u8, out var section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual(@"Content-Disposition: form-data; filename=""Transform-utf8.xsl""; name=""data""
Content-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>data form-data</data>"u8), Is.True);

        reader.Reset();

        Assert.That(reader.TryReadNextSectionByContentDisposition("attachment"u8, "data"u8, out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual(@"Content-Disposition: attachment; filename=""Transform-utf8.xsl""; name=data
Content-Type: text/xml"u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("<data>data attachment</data>"u8), Is.True);

        Assert.That(reader.TryReadNextSectionByContentDisposition("form-data"u8, "name"u8, out section), Is.True);
        Assert.That(span[section.Headers].SequenceEqual(@"Content-Disposition: form-data; name=""name"""u8), Is.True);
        Assert.That(span[section.Body].SequenceEqual("package name"u8), Is.True);
    }

    [Test]
    public void InvalidRead()
    {
        var boundary = new MultipartBoundary("------WebKitFormBoundarylng3rD4syfIK3fT9"u8);

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
            "------WebKitFormBoundarylng3rD4syfIK3fT9\r\n\r\n------WebKitFormBoundarylng3rD4syfIK3fT9--"u8)
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