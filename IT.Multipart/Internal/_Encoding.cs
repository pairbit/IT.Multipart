#if NETSTANDARD2_0

using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal static class _Encoding
{
    public static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars)
    {
        fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            return encoding.GetByteCount(charsPtr, chars.Length);
    }

    public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
        fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
    }

    public static unsafe int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            return encoding.GetCharCount(bytesPtr, bytes.Length);
    }

    public static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
        fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            return encoding.GetChars(bytesPtr, bytes.Length, charsPtr, chars.Length);
    }

    public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            return encoding.GetString(bytesPtr, bytes.Length);
    }
}

#endif