using IT.Multipart.Internal;

namespace IT.Multipart.Tests;

internal class MultipartSequenceReaderTest
{
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
"------WebKitFormBoundarylng3rD4syfIK3fT9--\r\n"u8 +
"123\r\n"u8;

        var boundary = new MultipartBoundary("\r\n------WebKitFormBoundarylng3rD4syfIK3fT9"u8.ToArray());
        var memory = span.ToArray().AsMemory();

        for (int i = 1; i <= memory.Length; i++)
        {
            var sequence = memory.SplitBySegments(i);

            var reader = new MultipartSequenceReader(boundary, sequence);

            Assert.That(reader.TryReadNextSection(out var section), Is.True);
            Assert.That(section.Headers.SequenceEqual("Content-Disposition: form-data; name=transform; filename=\"Transform;utf8.xsl\"\r\nContent-Type: text/xml"u8), Is.True);
            Assert.That(section.Body.SequenceEqual("<data>mydata</data>"u8), Is.True);

            Assert.That(reader.TryReadNextSection(out section), Is.True);
            Assert.That(section.Headers.SequenceEqual("Content-Disposition: form-data; name=\"name\""u8), Is.True);
            Assert.That(section.Body.SequenceEqual("package name"u8), Is.True);

            Assert.That(reader.TryReadNextSection(out section), Is.False);
            Assert.That(section, Is.EqualTo(default(MultipartSequenceSection)));

            Assert.That(reader.TryReadNextSection(out section), Is.False);
            Assert.That(section, Is.EqualTo(default(MultipartSequenceSection)));
        }
    }
}