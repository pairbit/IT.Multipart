using System;

namespace IT.Multipart;

//https://datatracker.ietf.org/doc/html/rfc5987
//Content-Disposition: form-data; name=file; filename=3fa92187-9eb1-4905-8ea8-70d1332162c0.xml; filename*=utf-8''3fa92187-9eb1-4905-8ea8-70d1332162c0.xml
//Content-Disposition: form-data; name="transform"; filename="Transform-utf8.xsl"
public ref struct MultipartContentDispositionReader
{
    private MultipartHeaderFieldsReader _reader;

    public readonly ReadOnlySpan<byte> Span => _reader.Span;

    public readonly int Offset => _reader.Offset;

    public MultipartContentDispositionReader(ReadOnlySpan<byte> span)
    {
        _reader = new MultipartHeaderFieldsReader(span);
    }

    public void Reset()
    {
        _reader.Reset();
    }

    public bool TryReadType(out Range value) => _reader.TryReadNextValue(out value);

    public bool IsFormData() => _reader.TryReadNextValue(out var type) && _reader.Span[type].SequenceEqual("form-data"u8);

    public bool IsAttachment() => _reader.TryReadNextValue(out var type) && _reader.Span[type].SequenceEqual("attachment"u8);

    public bool TryReadName(out Range value) => _reader.TryReadNextValueByName("name"u8, out value);

    public bool TryReadFileName(out Range value) => _reader.TryReadNextValueByName("filename"u8, out value);

    public bool TryReadFileNameStar(out Range value) => _reader.TryReadNextValueByName("filename*"u8, out value);

    public bool TryFindName(out Range value) => _reader.TryFindValueByName("name"u8, out value);

    public bool TryFindFileName(out Range value) => _reader.TryFindValueByName("filename"u8, out value);

    public bool TryFindFileNameStar(out Range value) => _reader.TryFindValueByName("filename*"u8, out value);
}