#if NET6_0_OR_GREATER
using System.Buffers;
using System.Text;

namespace IT.Multipart.Tests;

internal class MultipartBoundaryTest
{
    [Test]
    public void FromStringSegmentTest()
    {
        var writer = new ArrayBufferWriter<byte>(1024);

        var boundary = MultipartBoundary.FromStringSegment("\"----МойБоундари\"", writer);

        Assert.That(Encoding.UTF8.GetString(boundary.Span), Is.EqualTo("\r\n------МойБоундари"));

        boundary = MultipartBoundary.FromStringSegment("\"----MyBoundary\"", writer);

        Assert.That(Encoding.UTF8.GetString(boundary.Span), Is.EqualTo("\r\n------MyBoundary"));
    }
}
#endif