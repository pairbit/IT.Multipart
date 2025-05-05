using System.Net.Http.Headers;

namespace IT.Multipart.Tests;

internal class MultipartHeaderFieldsReaderTest
{
    [Test]
    public void TryReadNextValueTest()
    {
        var span = " form-data; name=\"transform\"; filename=\"Transform-utf8.xsl\""u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValue(out var value), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("name=\"transform\""u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("filename=\"Transform-utf8.xsl\""u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.False);
        Assert.That(value, Is.EqualTo(default(Range)));

        span = "form-data;name=transform;filename=Transform-utf8.xsl"u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("name=transform"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("filename=Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.False);
        Assert.That(value, Is.EqualTo(default(Range)));

        span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name=transform \n\r\t\v\f ; \n\r\t\v\f filename=Transform-utf8.xsl \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("name=transform"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("filename=Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.False);
        Assert.That(value, Is.EqualTo(default(Range)));

        span = " \n\r\t\v\f ; \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(value.Start, Is.EqualTo(value.End));
        Assert.That(span[value].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(value.Start, Is.EqualTo(value.End));
        Assert.That(span[value].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.False);
        Assert.That(value, Is.EqualTo(default(Range)));
    }
    
    [Test]
    public void TryReadNextValueTest_Quotes()
    {
        var span = "inline;filename=\"Transform ;utf8.xsl\""u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValue(out var value), Is.True);
        Assert.That(span[value].SequenceEqual("inline"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("filename=\"Transform"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("utf8.xsl\""u8), Is.True);

        var cd = ContentDispositionHeaderValue.Parse("inline;filename=\"Transform ;utf8.xsl\" ; f=b");
        Assert.That(cd.DispositionType, Is.EqualTo("inline"));
        Assert.That(cd.FileName, Is.EqualTo("\"Transform ;utf8.xsl\""));
        Assert.That(cd.ToString(), Is.EqualTo("inline; filename=\"Transform ;utf8.xsl\"; f=b"));

        reader.Reset();
        Assert.That(reader.TryReadNextField(out var field), Is.True);
        Assert.That(span[field.Name].IsEmpty, Is.True);
        Assert.That(span[field.Value].SequenceEqual("inline"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform ;utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out _), Is.False);

        span = "filename=\" Transform ; a; b; c; utf8.xsl \" \r\f\n  ;  f=\"1 ; 2 ; 3\"  "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual(" Transform ; a; b; c; utf8.xsl "u8), Is.True);
        
        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("f"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("1 ; 2 ; 3"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out _), Is.False);

        span = "ab=\"\"a\",\"b\"\""u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("ab"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("\"a\",\"b\""u8), Is.True);

        Assert.That(Assert.Throws<InvalidOperationException>(() => new MultipartHeaderFieldsReader("filename=\"a"u8).TryReadNextField(out _))
            .Message, Is.EqualTo(MultipartHeaderFieldsReader.QuoteNotFound().Message));

        Assert.That(Assert.Throws<InvalidOperationException>(() => new MultipartHeaderFieldsReader("filename=\"a;a=b"u8).TryReadNextField(out _))
            .Message, Is.EqualTo(MultipartHeaderFieldsReader.QuoteNotFound().Message));

        Assert.That(Assert.Throws<InvalidOperationException>(() => new MultipartHeaderFieldsReader("filename=\"a;\"a=b"u8).TryReadNextField(out _))
            .Message, Is.EqualTo(MultipartHeaderFieldsReader.QuoteInvalid().Message));
    }

    [Test]
    public void TryReadNextFieldTest()
    {
        var span = " form-data; name=transform; filename=\"Transform;utf8.xsl\"; filename*=utf-8''file%20name.jpg"u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextField(out var field), Is.True);
        Assert.That(field.Name, Is.EqualTo(default(Range)));
        Assert.That(span[field.Value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename*"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.False);
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        span = "form-data;name=\"transform\";filename=\"Transform;utf8.xsl\";filename*=\"utf-8''file%20name.jpg\""u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(field.Name, Is.EqualTo(default(Range)));
        Assert.That(span[field.Value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename*"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.False);
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name=transform \n\r\t\v\f ; \n\r\t\v\f filename=Transform-utf8.xsl \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(field.Name, Is.EqualTo(default(Range)));
        Assert.That(span[field.Value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.False);
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        span = "\n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.True);
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextField(out field), Is.False);
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        try
        {
            new MultipartHeaderFieldsReader("=val"u8).TryReadNextField(out _);
            Assert.Fail();
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Is.EqualTo(MultipartHeaderFieldsReader.NameNotFound().Message));
        }

        try
        {
            new MultipartHeaderFieldsReader(" \n\r\t\v\f =val"u8).TryReadNextField(out _);
            Assert.Fail();
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Is.EqualTo(MultipartHeaderFieldsReader.NameNotFound().Message));
        }
    }

    [Test]
    public void TryReadNextValueByNameTest()
    {
        var span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f "u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValueByName(""u8, out var value), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextValueByName("name"u8, out value), Is.True);
        Assert.That(span[value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryReadNextValueByName("filename"u8, out value), Is.True);
        Assert.That(span[value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);
    }

    [Test]
    public void TryFindValueByNameTest()
    {
        var span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f "u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryFindValueByName(""u8, out var value), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryFindValueByName("name"u8, out value), Is.True);
        Assert.That(span[value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryFindValueByName("filename"u8, out value), Is.True);
        Assert.That(span[value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);
    }

    [Test]
    public void TryFindValueByNameTest_Unorder()
    {
        var span = " \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f ; \n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f form-data \n\r\t\v\f "u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryFindValueByName(""u8, out var value), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        reader.Reset();
        Assert.That(reader.TryFindValueByName("name"u8, out value), Is.True);
        Assert.That(span[value].SequenceEqual("transform"u8), Is.True);
        
        reader.Reset();
        Assert.That(reader.TryFindValueByName("filename"u8, out value), Is.True);
        Assert.That(span[value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);
    }
}