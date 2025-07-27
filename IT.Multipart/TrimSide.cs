using System;

namespace IT.Multipart;

[Flags]
public enum TrimSide : byte
{
    None = 0,
    Start = 1,
    End = 2,
    StartEnd = 3,
}