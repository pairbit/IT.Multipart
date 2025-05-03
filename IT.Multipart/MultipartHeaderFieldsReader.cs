using System;

namespace IT.Multipart;

//https://datatracker.ietf.org/doc/html/rfc5987
//Content-Disposition: form-data; name=file; filename=3fa92187-9eb1-4905-8ea8-70d1332162c0.xml; filename*=utf-8''3fa92187-9eb1-4905-8ea8-70d1332162c0.xml
//Content-Disposition: form-data; name="transform"; filename="Transform-utf8.xsl"
public ref struct MultipartHeaderFieldsReader
{
    private const byte Quote = (byte)'"';
    private const byte Sep = (byte)';';
    private const byte NameSep = (byte)'=';

    private readonly ReadOnlySpan<byte> _span;
    private int _offset;

    public readonly ReadOnlySpan<byte> Span => _span;

    public readonly int Offset => _offset;

    public MultipartHeaderFieldsReader(ReadOnlySpan<byte> span)
    {
        _span = span;
        _offset = 0;
    }

    public void Reset()
    {
        _offset = 0;
    }

    public bool TryReadNextValue(out Range value)
    {
        var offset = _offset;
        var span = _span;
        if (span.Length <= offset)
        {
            value = default;
            return false;
        }
        span = span.Slice(offset);
#if DEBUG
        var spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        var sep = span.IndexOf(Sep);
        if (sep < 0) sep = span.Length;
        else
        {
            span = span.Slice(0, sep);
#if DEBUG
            spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        }
        var start = 0;
        for (; start < span.Length; start++)
        {
            if (!IsWhiteSpace(span[start])) break;
        }
        var end = sep - 1;
        for (; end >= start; end--)
        {
            if (!IsWhiteSpace(span[end])) break;
        }
        value = new(start + offset, end + offset + 1);
#if DEBUG
        spanUtf8 = System.Text.Encoding.UTF8.GetString(_span[value]);
#endif
        _offset = sep + offset + 1;
        return true;
    }

    public bool TryReadNextField(out MultipartHeaderField field)
    {
        if (!TryReadNextValue(out var value))
        {
            field = default;
            return false;
        }
        var span = _span[value];
#if DEBUG
        var spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        var nameSep = span.IndexOf(NameSep);
        if (nameSep < 0)
        {
            field = new() { Value = value };
        }
        else
        {
            if (nameSep == 0) throw NameNotFound();
#if DEBUG
            var nameUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(0, nameSep));
#endif
            var nameEnd = nameSep - 1;
            for (; nameEnd >= 0; nameEnd--)
            {
                if (!IsWhiteSpace(span[nameEnd])) break;
            }
            nameEnd++;
            if (nameEnd == 0) throw NameNotFound();
#if DEBUG
            nameUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(0, nameEnd));
#endif
            var valueStart = nameSep + 1;
#if DEBUG
            var valueUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(valueStart));
#endif
            for (; valueStart < span.Length; valueStart++)
            {
                if (!IsWhiteSpace(span[valueStart])) break;
            }
#if DEBUG
            valueUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(valueStart));
#endif
            var valueEnd = span.Length;
            if (span[valueStart] == Quote && span[valueEnd - 1] == Quote)
            {
                valueStart++;
                valueEnd--;
#if DEBUG
                valueUtf8 = System.Text.Encoding.UTF8.GetString(span[new Range(valueStart, valueEnd)]);
#endif
            }
            var nameStart = value.Start.Value;
            field = new()
            {
                Name = new(nameStart, nameEnd + nameStart),
                Value = new(valueStart + nameStart, valueEnd + nameStart)
            };
#if DEBUG
            nameUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Name]);
            valueUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Value]);
#endif
        }
        return true;
    }

    public bool TryReadNextValueByName(ReadOnlySpan<byte> name, out Range value)
    {
        if (TryReadNextField(out var field))
        {
#if DEBUG
            var nameUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Name]);
            var valueUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Value]);
#endif
            if (_span[field.Name].SequenceEqual(name))
            {
                value = field.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public bool TryFindValueByName(ReadOnlySpan<byte> name, out Range value)
    {
        var span = _span;
        while (TryReadNextField(out var field))
        {
#if DEBUG
            var nameUtf8 = System.Text.Encoding.UTF8.GetString(span[field.Name]);
            var valueUtf8 = System.Text.Encoding.UTF8.GetString(span[field.Value]);
#endif
            if (span[field.Name].SequenceEqual(name))
            {
                value = field.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static bool IsWhiteSpace(byte b) => b is
        (byte)' ' or //space
        (byte)'\n' or //newline
        (byte)'\r' or //carriage return
        (byte)'\t' or //horizontal tab
        (byte)'\v' or //vertical tab
        (byte)'\f'; //form feed

    internal static InvalidOperationException NameNotFound()
        => new("Invalid header field. Name not found");
}