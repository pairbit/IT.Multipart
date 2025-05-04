using System;

namespace IT.Multipart;

public readonly struct MultipartContentDisposition
{
    public Range Type { get; init; }

    public Range Name { get; init; }

    public Range FileName { get; init; }

    public Range FileNameStar { get; init; }
}