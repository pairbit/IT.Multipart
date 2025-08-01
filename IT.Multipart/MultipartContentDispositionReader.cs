using System;

namespace IT.Multipart;

//https://datatracker.ietf.org/doc/html/rfc5987
//Content-Disposition: form-data; name=file; filename=3fa92187-9eb1-4905-8ea8-70d1332162c0.xml; filename*=utf-8''3fa92187-9eb1-4905-8ea8-70d1332162c0.xml
//Content-Disposition: form-data; name="transform"; filename="Transform;utf8.xsl"
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

    public bool IsType(ReadOnlySpan<byte> value) => _reader.TryReadNextValue(out var type) && _reader.Span[type].SequenceEqual(value);

    public bool IsFormData() => IsType("form-data"u8);

    public bool IsAttachment() => IsType("attachment"u8);

    public MultipartReadingStatus ReadName(out Range value) => _reader.ReadNextValueByName("name"u8, out value);

    public MultipartReadingStatus ReadFileName(out Range value) => _reader.ReadNextValueByName("filename"u8, out value);

    public MultipartReadingStatus ReadFileNameStar(out Range value) => _reader.ReadNextValueByName("filename*"u8, out value);

    public bool TryRead(out MultipartContentDisposition value)
    {
        if (!TryReadType(out var type) || type.Start.Value == type.End.Value)
        {
            value = default;
            return false;
        }

        if (_reader.ReadNextField(out var field) != MultipartReadingStatus.Done)
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
            if (_reader.ReadNextField(out field) != MultipartReadingStatus.Done)
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
            if (_reader.ReadNextField(out field) != MultipartReadingStatus.Done)
            {
                value = new() { Type = type, Name = name, FileName = fileName };
                return true;
            }
            fieldName = span[field.Name];
        }

        if (fieldName.SequenceEqual("filename*"u8) && !_reader.TryReadNextValue(out _))
        {
            value = new() { Type = type, Name = name, FileName = fileName, FileNameStar = field.Value };
            return true;
        }

        value = default;
        return false;
    }

    public MultipartReadingStatus FindName(out Range value) => _reader.FindValueByName("name"u8, out value);

    public MultipartReadingStatus FindFileName(out Range value) => _reader.FindValueByName("filename"u8, out value);

    public MultipartReadingStatus FindFileNameStar(out Range value) => _reader.FindValueByName("filename*"u8, out value);
}