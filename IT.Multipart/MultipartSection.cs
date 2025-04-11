using System;

namespace IT.Multipart;

public readonly struct MultipartSection
{
    public Range Headers { get; init; }

    public Range Body { get; init; }
}