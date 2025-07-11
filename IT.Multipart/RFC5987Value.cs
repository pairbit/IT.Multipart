using System;

namespace IT.Multipart;

/// <summary>
/// encoding'language'my%20string
/// </summary>
public readonly struct RFC5987Value
{
    private readonly int _firstIndex;
    private readonly int _secondIndex;

    public int CharsetEnd => _firstIndex;

    public int LanguageStart => _firstIndex + 1;

    public int LanguageLength => _secondIndex - (_firstIndex + 1);

    public Range Language => new(_firstIndex + 1, _secondIndex);

    public int EncodedStart => _secondIndex + 1;

    public RFC5987Value(int firstIndex, int secondIndex)
    {
        if (firstIndex <= 0) throw new ArgumentOutOfRangeException(nameof(firstIndex));
        if (secondIndex <= firstIndex) throw new ArgumentOutOfRangeException(nameof(secondIndex));

        _firstIndex = firstIndex;
        _secondIndex = secondIndex;
    }
}