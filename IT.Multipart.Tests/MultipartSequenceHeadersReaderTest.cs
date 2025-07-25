using IT.Multipart.Internal;

namespace IT.Multipart.Tests;

internal class MultipartSequenceHeadersReaderTest
{
    //[Test]
    public void TryReadNextHeaderTest()
    {
        var span = "Content-Disposition: form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\"\r\nContent-Type: text/xml"u8;
        var memory = span.ToArray().AsMemory();

        for (int i = 0; i < memory.Length; i++)
        {
            var sequence = memory.SplitBySegments(i);

            var reader = new MultipartSequenceHeadersReader(sequence);

            Assert.That(reader.TryReadNextHeader(out var header), Is.True);
            Assert.That(header.Name.SequenceEqual("Content-Disposition"u8), Is.True);
            Assert.That(header.Value.SequenceEqual("form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\""u8), Is.True);

            Assert.That(reader.TryReadNextHeader(out header), Is.True);
            Assert.That(header.Name.SequenceEqual("Content-Type"u8), Is.True);
            Assert.That(header.Value.SequenceEqual("text/xml"u8), Is.True);

            Assert.That(reader.TryReadNextHeader(out header), Is.False);
            Assert.That(header, Is.EqualTo(default(MultipartSequenceHeader)));

            reader.Reset();
            Assert.That(reader.TryReadNextContentDisposition(out var value), Is.True);
            Assert.That(value.SequenceEqual("form-data; name=\"transform\"; filename=\"Transform - utf8.xsl\""u8), Is.True);

            Assert.That(reader.TryReadNextContentType(out value), Is.True);
            Assert.That(value.SequenceEqual("text/xml"u8), Is.True);
        }

        span = "Content-Type:text/xml"u8;
        memory = span.ToArray().AsMemory();
        for (int i = 0; i < memory.Length; i++)
        {
            var sequence = memory.SplitBySegments(i);
            var reader = new MultipartSequenceHeadersReader(sequence);

            Assert.That(reader.TryReadNextHeader(out var header), Is.True);
            Assert.That(header.Name.SequenceEqual("Content-Type"u8), Is.True);
            Assert.That(header.Value.SequenceEqual("text/xml"u8), Is.True);
        }

        span = " Content-Disposition : \n\r\t\v\f form-data; name=transform; filename=\"Transform - utf8.xsl\" \n\r\t\v\f \r\n Content-Type : \n\r\t\v\f text/xml \n\r\t\v\f "u8;
        memory = span.ToArray().AsMemory();
        for (int i = 0; i < memory.Length; i++)
        {
            var sequence = memory.SplitBySegments(i);
            var reader = new MultipartSequenceHeadersReader(sequence);

            Assert.That(reader.TryReadNextHeader(out var header), Is.True);
            Assert.That(header.Name.SequenceEqual(" Content-Disposition "u8), Is.True);
            Assert.That(header.Value.SequenceEqual("form-data; name=transform; filename=\"Transform - utf8.xsl\""u8), Is.True);

            Assert.That(reader.TryReadNextHeader(out header), Is.True);
            Assert.That(header.Name.SequenceEqual(" Content-Type "u8), Is.True);
            Assert.That(header.Value.SequenceEqual("text/xml"u8), Is.True);
        }

        span = "empty:\r\nempty2:"u8;
        memory = span.ToArray().AsMemory();
        for (int i = 0; i < memory.Length; i++)
        {
            var sequence = memory.SplitBySegments(i);
            var reader = new MultipartSequenceHeadersReader(sequence);

            Assert.That(reader.TryReadNextHeader(out var header), Is.True);
            Assert.That(header.Name.SequenceEqual("empty"u8), Is.True);
            Assert.That(header.Value.IsEmpty, Is.True);

            Assert.That(reader.TryReadNextHeader(out header), Is.True);
            Assert.That(header.Name.SequenceEqual("empty2"u8), Is.True);
            Assert.That(header.Value.IsEmpty, Is.True);
        }

        span = "empty: \n\r\t\v\f \r\nempty2: \n\r\t\v\f "u8;
        memory = span.ToArray().AsMemory();
        for (int i = 0; i < memory.Length; i++)
        {
            var sequence = memory.SplitBySegments(i);
            var reader = new MultipartSequenceHeadersReader(sequence);

            Assert.That(reader.TryReadNextHeader(out var header), Is.True);
            Assert.That(header.Name.SequenceEqual("empty"u8), Is.True);
            Assert.That(header.Value.IsEmpty, Is.True);

            Assert.That(reader.TryReadNextHeader(out header), Is.True);
            Assert.That(header.Name.SequenceEqual("empty2"u8), Is.True);
            Assert.That(header.Value.IsEmpty, Is.True);
        }

    }
}