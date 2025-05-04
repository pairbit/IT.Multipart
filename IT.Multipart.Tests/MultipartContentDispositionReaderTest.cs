namespace IT.Multipart.Tests;

internal class MultipartContentDispositionReaderTest
{
    [Test]
    public void TryReadTest()
    {
        var span = " form-data; name=transform; filename=\"Transform-utf8.xsl\"; filename*=utf-8''file%20name.jpg"u8;
        var reader = new MultipartContentDispositionReader(span);

        Assert.That(reader.Offset, Is.Zero);
        Assert.That(reader.IsFormData(), Is.True);

        Assert.That(reader.Offset, Is.Not.Zero);
        Assert.That(reader.TryReadName(out var name), Is.True);
        Assert.That(span[name].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryReadFileName(out var filename), Is.True);
        Assert.That(span[filename].SequenceEqual("Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.TryReadFileNameStar(out var filenamestar), Is.True);
        Assert.That(span[filenamestar].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);

        reader.Reset();
        Assert.That(reader.Offset, Is.Zero);

        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(span[cd.Name].SequenceEqual("transform"u8), Is.True);
        Assert.That(span[cd.FileName].SequenceEqual("Transform-utf8.xsl"u8), Is.True);
        Assert.That(span[cd.FileNameStar].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);
    }

    [Test]
    public void TryReadTest_Type()
    {
        var span = " form-data "u8;
        var reader = new MultipartContentDispositionReader(span);
        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(cd.Name, Is.EqualTo(default(Range)));
        Assert.That(cd.FileName, Is.EqualTo(default(Range)));
        Assert.That(cd.FileNameStar, Is.EqualTo(default(Range)));
    }

    [Test]
    public void TryReadTest_Name()
    {
        var span = " form-data; name=transform"u8;
        var reader = new MultipartContentDispositionReader(span);
        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(span[cd.Name].SequenceEqual("transform"u8), Is.True);
        Assert.That(cd.FileName, Is.EqualTo(default(Range)));
        Assert.That(cd.FileNameStar, Is.EqualTo(default(Range)));
    }

    [Test]
    public void TryReadTest_FileName()
    {
        var span = " form-data; filename=\"Transform-utf8.xsl\""u8;
        var reader = new MultipartContentDispositionReader(span);
        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(cd.Name, Is.EqualTo(default(Range)));
        Assert.That(span[cd.FileName].SequenceEqual("Transform-utf8.xsl"u8), Is.True);
        Assert.That(cd.FileNameStar, Is.EqualTo(default(Range)));
    }

    [Test]
    public void TryReadTest_FileNameStar()
    {
        var span = " form-data; filename*=utf-8''file%20name.jpg"u8;
        var reader = new MultipartContentDispositionReader(span);
        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(cd.Name, Is.EqualTo(default(Range)));
        Assert.That(cd.FileName, Is.EqualTo(default(Range)));
        Assert.That(span[cd.FileNameStar].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);
    }

    [Test]
    public void TryReadTest_Name_FileName()
    {
        var span = " form-data; name=transform; filename=\"Transform-utf8.xsl\""u8;
        var reader = new MultipartContentDispositionReader(span);
        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(span[cd.Name].SequenceEqual("transform"u8), Is.True);
        Assert.That(span[cd.FileName].SequenceEqual("Transform-utf8.xsl"u8), Is.True);
        Assert.That(cd.FileNameStar, Is.EqualTo(default(Range)));
    }

    [Test]
    public void TryReadTest_Name_FileNameStar()
    {
        var span = " form-data; name=transform; filename*=utf-8''file%20name.jpg"u8;
        var reader = new MultipartContentDispositionReader(span);
        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(span[cd.Name].SequenceEqual("transform"u8), Is.True);
        Assert.That(cd.FileName, Is.EqualTo(default(Range)));
        Assert.That(span[cd.FileNameStar].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);
    }

    [Test]
    public void TryReadTest_FileName_FileNameStar()
    {
        var span = " form-data; filename=\"Transform-utf8.xsl\"; filename*=utf-8''file%20name.jpg"u8;
        var reader = new MultipartContentDispositionReader(span);
        Assert.That(reader.TryRead(out var cd), Is.True);
        Assert.That(span[cd.Type].SequenceEqual("form-data"u8), Is.True);
        Assert.That(cd.Name, Is.EqualTo(default(Range)));
        Assert.That(span[cd.FileName].SequenceEqual("Transform-utf8.xsl"u8), Is.True);
        Assert.That(span[cd.FileNameStar].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);
    }

    [Test]
    public void TryFindTest()
    {
        var span = " form-data; name=transform; filename=\"Transform-utf8.xsl\"; filename*=utf-8''file%20name.jpg"u8;
        var reader = new MultipartContentDispositionReader(span);

        Assert.That(reader.Offset, Is.Zero);
        Assert.That(reader.IsFormData(), Is.True);

        Assert.That(reader.Offset, Is.Not.Zero);
        Assert.That(reader.TryFindName(out var name), Is.True);
        Assert.That(span[name].SequenceEqual("transform"u8), Is.True);

        Assert.That(reader.TryFindFileName(out var filename), Is.True);
        Assert.That(span[filename].SequenceEqual("Transform-utf8.xsl"u8), Is.True);

        Assert.That(reader.TryFindFileNameStar(out var filenamestar), Is.True);
        Assert.That(span[filenamestar].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);

        reader.Reset();
        Assert.That(reader.Offset, Is.Zero);
    }

    [Test]
    public void TryFindTest_Unorder()
    {
        var span = " form-data; filename*=utf-8''file%20name.jpg; filename=\"Transform-utf8.xsl\"; name=transform"u8;
        var reader = new MultipartContentDispositionReader(span);

        Assert.That(reader.Offset, Is.Zero);
        Assert.That(reader.IsFormData(), Is.True);

        Assert.That(reader.Offset, Is.Not.Zero);
        Assert.That(reader.TryFindName(out var name), Is.True);
        Assert.That(span[name].SequenceEqual("transform"u8), Is.True);

        reader.Reset();
        Assert.That(reader.TryFindFileName(out var filename), Is.True);
        Assert.That(span[filename].SequenceEqual("Transform-utf8.xsl"u8), Is.True);

        reader.Reset();
        Assert.That(reader.TryFindFileNameStar(out var filenamestar), Is.True);
        Assert.That(span[filenamestar].SequenceEqual("utf-8''file%20name.jpg"u8), Is.True);

        reader.Reset();
        Assert.That(reader.Offset, Is.Zero);
    }
}