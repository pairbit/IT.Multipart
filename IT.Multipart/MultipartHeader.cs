using System;

namespace IT.Multipart;

public readonly struct MultipartHeader
{
    public Range Name { get; init; }

    public Range Value { get; init; }
}