using System.Text;

namespace IT.Multipart.Tests;

internal class TrimOptionsTest
{
    [Test]
    public void Test()
    {
        Assert.That(TrimOptions.None.Side, Is.EqualTo(TrimSide.None));
        Assert.That(!TrimOptions.None.HasStart && !TrimOptions.None.HasEnd, Is.True);

        var max = TrimOptions.Max;
        Assert.That(max.HasStart && max.HasEnd, Is.True);
        Assert.That(max.Side, Is.EqualTo(TrimSide.StartEnd));
        Assert.That(TrimOptions.MaxStart.HasStart && !TrimOptions.MaxStart.HasEnd, Is.True);
        Assert.That(!TrimOptions.MaxEnd.HasStart && TrimOptions.MaxEnd.HasEnd, Is.True);

        for (int i = 0; i <= 255; i++)
        {
            var by = (byte)i;
            var isWhiteSpace = IsWhiteSpace(by);
            Assert.That(max.Contains(by), Is.EqualTo(isWhiteSpace));

            var ch = (char)by;
            if (char.IsWhiteSpace(ch) != isWhiteSpace)
            {
                Assert.Fail($"Is not WhiteSpace '{by}'");
            }
        }

        var min = TrimOptions.Min;
        Assert.That(min.HasStart && min.HasEnd, Is.True);
        Assert.That(min.Side, Is.EqualTo(TrimSide.StartEnd));
        Assert.That(TrimOptions.MinStart.HasStart && !TrimOptions.MinStart.HasEnd, Is.True);
        Assert.That(!TrimOptions.MinEnd.HasStart && TrimOptions.MinEnd.HasEnd, Is.True);

        for (int i = 0; i <= 255; i++)
        {
            var by = (byte)i;
            Assert.That(min.Contains(by), Is.EqualTo(by == ' '));
        }
    }

    //[Test]
    public void GenerateMap()
    {
        var sb = new StringBuilder();
        sb.Append("[");
        for (int i = 0; i <= 255; i++)
        {
            sb.Append("/*");
            sb.Append(i);
            sb.Append("*/");
            if (IsWhiteSpace((byte)i))
            {
                sb.Append("true");
            }
            else
            {
                sb.Append("false");
            }
            if (i != 255)
                sb.AppendLine(",");
        }
        sb.Append("]");
        var str = sb.ToString();
    }

    private static bool IsWhiteSpace(byte b) => b is
        (byte)' ' or //space
        (byte)'\n' or //newline
        (byte)'\r' or //carriage return
        (byte)'\t' or //horizontal tab
        (byte)'\v' or //vertical tab
        (byte)'\f' or //form feed
        (byte)'\u0085' or
        (byte)' ';

}