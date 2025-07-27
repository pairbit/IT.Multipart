using System;
using System.Diagnostics;

namespace IT.Multipart;

//https://datatracker.ietf.org/doc/html/rfc5987
//Content-Disposition: form-data; name=file; filename=3fa92187-9eb1-4905-8ea8-70d1332162c0.xml; filename*=utf-8''3fa92187-9eb1-4905-8ea8-70d1332162c0.xml
//Content-Disposition: form-data; name="transform"; filename="Transform;utf8.xsl"
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
        => TryReadNextValue(out value, TrimOptions.MinStart);

    public bool TryReadNextValue(out Range value, TrimOptions trim)
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
        if (trim.HasStart)
        {
            Debug.Assert(start < span.Length);
            trim.ClampStart(span, ref start);
        }
        var end = sep - 1;
        if (trim.HasEnd && end >= start)
            trim.ClampEnd(span, start, ref end);
        value = new(start + offset, end + offset + 1);
#if DEBUG
        spanUtf8 = System.Text.Encoding.UTF8.GetString(_span[value]);
#endif
        _offset = sep + offset + 1;
        return true;
    }

    public MultipartReadingStatus ReadNextField(out MultipartHeaderField field)
        => ReadNextField(out field, TrimOptions.MinStart, TrimOptions.None);

    public MultipartReadingStatus ReadNextField(out MultipartHeaderField field,
        TrimOptions trim, TrimOptions trimField)
    {
        if (!TryReadNextValue(out var value, trim))
        {
            field = default;
            return MultipartReadingStatus.End;
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
            if (nameSep == 0)
            {
                field = default;
                return MultipartReadingStatus.HeaderFieldNameNotFound;
            }
#if DEBUG
            var nameUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(0, nameSep));
#endif
            var nameEnd = nameSep - 1;
            if (trimField.HasEnd && nameEnd >= 0)
                trimField.ClampEnd(span, 0, ref nameEnd);

            nameEnd++;
            if (nameEnd == 0)
            {
                field = default;
                return MultipartReadingStatus.HeaderFieldNameNotFound;
            }
#if DEBUG
            nameUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(0, nameEnd));
#endif
            var valueStart = nameSep + 1;
#if DEBUG
            var valueUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(valueStart));
#endif
            if (trimField.HasStart && valueStart < span.Length)
                trimField.ClampStart(span, ref valueStart);
#if DEBUG
            valueUtf8 = System.Text.Encoding.UTF8.GetString(span.Slice(valueStart));
#endif
            var nameStart = value.Start.Value;
            var valueEnd = span.Length;
            if (span[valueStart] == Quote)
            {
                if (span[valueEnd - 1] == Quote)
                {
                    valueEnd = valueEnd + nameStart - 1;
                }
                else
                {
                    var status = ReadNextQuote(trim, out valueEnd);
                    if (status != MultipartReadingStatus.Done)
                    {
                        field = default;
                        return status;
                    }
                    valueEnd--;
                }
                valueStart++;
            }
            else
            {
                valueEnd += nameStart;
            }
            field = new()
            {
                Name = new(nameStart, nameEnd + nameStart),
                Value = new(valueStart + nameStart, valueEnd)
            };
#if DEBUG
            nameUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Name]);
            valueUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Value]);
#endif
        }
        return MultipartReadingStatus.Done;
    }

    public MultipartReadingStatus ReadNextValueByName(ReadOnlySpan<byte> name, out Range value)
        => ReadNextValueByName(name, out value, TrimOptions.MinStart, TrimOptions.None);

    public MultipartReadingStatus ReadNextValueByName(ReadOnlySpan<byte> name, out Range value,
        TrimOptions trim, TrimOptions trimField)
    {
        var status = ReadNextField(out var field, trim, trimField);
        if (status != MultipartReadingStatus.Done)
        {
            value = default;
            return status;
        }

#if DEBUG
        var nameUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Name]);
        var valueUtf8 = System.Text.Encoding.UTF8.GetString(_span[field.Value]);
#endif
        if (!_span[field.Name].SequenceEqual(name))
        {
            value = default;
            return MultipartReadingStatus.HeaderFieldNameNotSame;
        }

        value = field.Value;
        return MultipartReadingStatus.Done;
    }

    public MultipartReadingStatus FindValueByName(ReadOnlySpan<byte> name, out Range value)
        => FindValueByName(name, out value, TrimOptions.MinStart, TrimOptions.None);

    public MultipartReadingStatus FindValueByName(ReadOnlySpan<byte> name, out Range value,
        TrimOptions trim, TrimOptions trimField)
    {
        var span = _span;
        MultipartReadingStatus status;
        do
        {
            status = ReadNextField(out var field, trim, trimField);
            if (status != MultipartReadingStatus.Done)
            {
                value = default;
                break;
            }
#if DEBUG
            var nameUtf8 = System.Text.Encoding.UTF8.GetString(span[field.Name]);
            var valueUtf8 = System.Text.Encoding.UTF8.GetString(span[field.Value]);
#endif
            if (span[field.Name].SequenceEqual(name))
            {
                value = field.Value;
                status = MultipartReadingStatus.Done;
                break;
            }
        } while (true);

        return status;
    }

    /*
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
            var nameStart = value.Start.Value;
            var valueEnd = span.Length;
            if (span[valueStart] == Quote)
            {
                valueEnd = span[valueEnd - 1] == Quote ? valueEnd + nameStart - 1 : ReadNextQuote() - 1;
                valueStart++;
            }
            else
            {
                valueEnd += nameStart;
            }
            field = new()
            {
                Name = new(nameStart, nameEnd + nameStart),
                Value = new(valueStart + nameStart, valueEnd)
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
    */

    private MultipartReadingStatus ReadNextQuote(TrimOptions trim, out int index)
    {
        var offset = _offset;
        var span = _span;
        if (span.Length <= offset)
        {
            index = 0;
            return MultipartReadingStatus.HeaderFieldValueEndQuoteNotFound;
        }
        span = span.Slice(offset);
#if DEBUG
        var spanUtf8 = System.Text.Encoding.UTF8.GetString(span);
#endif
        var sep = span.IndexOf(Quote);
        if (sep < 0)
        {
            index = 0;
            return MultipartReadingStatus.HeaderFieldValueEndQuoteNotFound;
        }
        sep++;
        var end = sep;
        if (end < span.Length)
        {
            if (trim.HasEnd)
            {
                do
                {
                    var token = span[end];
                    if (!trim.Contains(token))
                    {
                        if (token != Sep)
                        {
                            index = 0;
                            return MultipartReadingStatus.HeaderFieldValueEndQuoteInvalid;
                        }
                        break;
                    }
                } while (++end < span.Length);
            }
            else if (span[end] != Sep)
            {
                index = 0;
                return MultipartReadingStatus.HeaderFieldValueEndQuoteInvalid;
            }
            end++;
        }
        _offset = offset + end;
        index = offset + sep;
        return MultipartReadingStatus.Done;
    }

    private static bool IsWhiteSpace(byte b) => b is
        (byte)' ' or //space
        (byte)'\n' or //newline
        (byte)'\r' or //carriage return
        (byte)'\t' or //horizontal tab
        (byte)'\v' or //vertical tab
        (byte)'\f'; //form feed

    internal static InvalidOperationException QuoteNotFound()
        => new("Invalid header field. Quote not found");

    internal static InvalidOperationException QuoteInvalid()
        => new("Invalid header field. Quote is invalid");
}