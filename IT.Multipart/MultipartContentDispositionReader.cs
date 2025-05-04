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

    public bool TryRead(out MultipartContentDisposition value)
    {
        if (!TryReadType(out var type))
        {
            value = default;
            return false;
        }

        if (!_reader.TryReadNextField(out var field))
        {
            value = new() { Type = type };
            return true;
        }

        Range name = default;
        var span = _reader.Span;
        var fieldName = span[field.Name];
        if (fieldName.SequenceEqual("name"u8))
        {
            name = field.Value;
            if (!_reader.TryReadNextField(out field))
            {
                value = new() { Type = type, Name = name };
                return true;
            }
            fieldName = span[field.Name];
        }

        Range fileName = default;
        if (fieldName.SequenceEqual("filename"u8))
        {
            fileName = field.Value;
            if (!_reader.TryReadNextField(out field))
            {
                value = new() { Type = type, Name = name, FileName = fileName };
                return true;
            }
            fieldName = span[field.Name];
        }

        Range fileNameStar = default;
        if (fieldName.SequenceEqual("filename*"u8)) fileNameStar = field.Value;

        value = new() { Type = type, Name = name, FileName = fileName, FileNameStar = fileNameStar };
        return true;
    }

    public bool TryFindName(out Range value) => _reader.TryFindValueByName("name"u8, out value);

    public bool TryFindFileName(out Range value) => _reader.TryFindValueByName("filename"u8, out value);

    public bool TryFindFileNameStar(out Range value) => _reader.TryFindValueByName("filename*"u8, out value);
}