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
}