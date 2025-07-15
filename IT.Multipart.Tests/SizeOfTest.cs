using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Multipart.Tests;

internal class SizeOfTest
{
    [Test]
    public void SizeTest()
    {
        Assert.That(Unsafe.SizeOf<RFC5987Value>(), Is.EqualTo(8));
        Assert.That(Unsafe.SizeOf<MultipartHeader>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<MultipartHeaderField>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<MultipartSection>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<MultipartContentDisposition>(), Is.EqualTo(32));

        Assert.That(Unsafe.SizeOf<SequencePosition>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<ReadOnlySequence<byte>>(), Is.EqualTo(
#if NET
            24
#else
            32
#endif
            ));

#if NET9_0_OR_GREATER
        Assert.That(Unsafe.SizeOf<MultipartBoundary>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<MultipartHeadersReader>(), Is.EqualTo(24));
        Assert.That(Unsafe.SizeOf<MultipartHeaderFieldsReader>(), Is.EqualTo(24));
        Assert.That(Unsafe.SizeOf<MultipartReader>(), Is.EqualTo(40));
#endif
    }
}