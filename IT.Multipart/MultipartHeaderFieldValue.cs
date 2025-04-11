using System;

namespace IT.Multipart;

internal readonly struct MultipartHeaderFieldValue
{
    public Range Charset { get; init; }

    public Range Language { get; init; }

    public Range Encoded { get; init; }
}