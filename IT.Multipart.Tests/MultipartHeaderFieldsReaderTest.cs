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

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.None), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.None), Is.True);
        Assert.That(span[value].SequenceEqual("name=transform"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.None), Is.True);
        Assert.That(span[value].SequenceEqual("filename=Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.False);
        Assert.That(value, Is.EqualTo(default(Range)));

        span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name=transform \n\r\t\v\f ; \n\r\t\v\f filename=Transform-utf8.xsl \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.Max), Is.True);
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.Max), Is.True);
        Assert.That(span[value].SequenceEqual("name=transform"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.Max), Is.True);
        Assert.That(span[value].SequenceEqual("filename=Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.False);
        Assert.That(value, Is.EqualTo(default(Range)));

        span = " \n\r\t\v\f ; \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.MaxStart), Is.True);
        Assert.That(value.Start, Is.EqualTo(value.End));
        Assert.That(span[value].IsEmpty, Is.True);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.MaxStart), Is.True);
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

        Assert.That(reader.TryReadNextValue(out var value, TrimOptions.None), Is.True);
        Assert.That(span[value].SequenceEqual("inline"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value, TrimOptions.None), Is.True);
        Assert.That(span[value].SequenceEqual("filename=\"Transform "u8), Is.True);

        Assert.That(reader.TryReadNextValue(out value), Is.True);
        Assert.That(span[value].SequenceEqual("utf8.xsl\""u8), Is.True);

        var cd = ContentDispositionHeaderValue.Parse("inline;filename=\"Transform ;utf8.xsl\" ; f=b");
        Assert.That(cd.DispositionType, Is.EqualTo("inline"));
        Assert.That(cd.FileName, Is.EqualTo("\"Transform ;utf8.xsl\""));
        Assert.That(cd.ToString(), Is.EqualTo("inline; filename=\"Transform ;utf8.xsl\"; f=b"));

        reader.Reset();
        Assert.That(reader.ReadNextField(out var field, TrimOptions.None, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].IsEmpty, Is.True);
        Assert.That(span[field.Value].SequenceEqual("inline"u8), Is.True);

        Assert.That(reader.ReadNextField(out field, TrimOptions.None, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform ;utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out _), Is.False);

        span = "filename=\" Transform ; a; b; c; utf8.xsl \" \r\f\n  ;  f=\"1 ; 2 ; 3\"  "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.ReadNextField(out field, TrimOptions.MaxEnd, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual(" Transform ; a; b; c; utf8.xsl "u8), Is.True);
        
        Assert.That(reader.ReadNextField(out field, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("f"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("1 ; 2 ; 3"u8), Is.True);

        Assert.That(reader.TryReadNextValue(out _), Is.False);

        span = "ab=\"\"a\",\"b\"\""u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.ReadNextField(out field, TrimOptions.None, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("ab"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("\"a\",\"b\""u8), Is.True);

        Assert.That(Assert.Throws<InvalidOperationException>(() => new MultipartHeaderFieldsReader("filename=\"a"u8).ReadNextField(out _))
            .Message, Is.EqualTo(MultipartHeaderFieldsReader.QuoteNotFound().Message));

        Assert.That(Assert.Throws<InvalidOperationException>(() => new MultipartHeaderFieldsReader("filename=\"a;a=b"u8).ReadNextField(out _))
            .Message, Is.EqualTo(MultipartHeaderFieldsReader.QuoteNotFound().Message));

        Assert.That(Assert.Throws<InvalidOperationException>(() => new MultipartHeaderFieldsReader("filename=\"a;\"a=b"u8).ReadNextField(out _))
            .Message, Is.EqualTo(MultipartHeaderFieldsReader.QuoteInvalid().Message));
    }

    [Test]
    public void ReadNextFieldTest()
    {
        var span = " form-data; name=transform; filename=\"Transform;utf8.xsl\"; filename*=utf-8''file%20name.jpg"u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.ReadNextField(out var field), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(field.Name, Is.EqualTo(default(Range)));
        Assert.That(span[field.Value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);

        Assert.That(reader.ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename*"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);

        Assert.That(reader.ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.End));
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        span = "form-data;name=\"transform\";filename=\"Transform;utf8.xsl\";filename*=\"utf-8''file%20name.jpg\""u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.ReadNextField(out field, TrimOptions.None, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(field.Name, Is.EqualTo(default(Range)));
        Assert.That(span[field.Value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.ReadNextField(out field, TrimOptions.None, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.ReadNextField(out field, TrimOptions.None, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);

        Assert.That(reader.ReadNextField(out field, TrimOptions.None, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename*"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);

        Assert.That(reader.ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.End));
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name=transform \n\r\t\v\f ; \n\r\t\v\f filename=Transform-utf8.xsl \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.ReadNextField(out field, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(field.Name, Is.EqualTo(default(Range)));
        Assert.That(span[field.Value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.ReadNextField(out field, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.ReadNextField(out field, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.End));
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        span = "\n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f "u8;
        reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.ReadNextField(out field, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("name"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.ReadNextField(out field, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[field.Name].SequenceEqual("filename"u8), Is.True);
        Assert.That(span[field.Value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);

        Assert.That(reader.ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.End));
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        Assert.That(new MultipartHeaderFieldsReader("=val"u8).ReadNextField(out field), Is.EqualTo(MultipartReadingStatus.HeaderFieldNameNotFound));
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));

        Assert.That(new MultipartHeaderFieldsReader(" \n\r\t\v\f =val"u8).ReadNextField(out field, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.HeaderFieldNameNotFound));
        Assert.That(field, Is.EqualTo(default(MultipartHeaderField)));
    }

    [Test]
    public void ReadNextValueByNameTest()
    {
        var span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f "u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.ReadNextValueByName(""u8, out var value, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.ReadNextValueByName("name"u8, out value, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.ReadNextValueByName("filename"u8, out value, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);
    }

    [Test]
    public void FindValueByNameTest()
    {
        var span = " \n\r\t\v\f form-data \n\r\t\v\f ; \n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f "u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.FindValueByName(""u8, out var value, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        Assert.That(reader.FindValueByName("name"u8, out value, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.FindValueByName("filename"u8, out value, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);
    }

    [Test]
    public void FindValueByNameTest_Unorder()
    {
        var span = " \n\r\t\v\f filename \n\r\t\v\f = \n\r\t\v\f \"Transform;utf8.xsl\" \n\r\t\v\f ; \n\r\t\v\f name \n\r\t\v\f = \n\r\t\v\f transform \n\r\t\v\f ; \n\r\t\v\f form-data \n\r\t\v\f "u8;
        var reader = new MultipartHeaderFieldsReader(span);

        Assert.That(reader.FindValueByName(""u8, out var value, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("form-data"u8), Is.True);

        reader.Reset();
        Assert.That(reader.FindValueByName("name"u8, out value, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("transform"u8), Is.True);
        
        reader.Reset();
        Assert.That(reader.FindValueByName("filename"u8, out value, TrimOptions.Max, TrimOptions.Max), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("Transform;utf8.xsl"u8), Is.True);

        reader.Reset();
        //TODO: баг или фича??
        Assert.That(reader.FindValueByName(""u8, out value, TrimOptions.Max, TrimOptions.None), Is.EqualTo(MultipartReadingStatus.Done));
        Assert.That(span[value].SequenceEqual("utf8.xsl\""u8), Is.True);
    }
}