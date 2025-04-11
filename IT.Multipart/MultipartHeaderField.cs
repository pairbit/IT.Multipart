using System;

namespace IT.Multipart;

public readonly struct MultipartHeaderField
{
    public Range Name { get; init; }

    public Range Value { get; init; }
}