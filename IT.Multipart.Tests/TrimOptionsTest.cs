using System.Globalization;
using System.Text;

namespace IT.Multipart.Tests;

internal class TrimOptionsTest
{
    [Test]
    public void Test()
    {
        var ch = ' ';
        var b = (byte)ch;

        var opt = TrimOptions.Default;
        for (int i = 0; i <= 255; i++)
        {
            var by = (byte)i;
            Assert.That(opt.Contains(by), Is.EqualTo(IsWhiteSpace(by)));
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
        var str= sb.ToString();
    }

    private static bool IsWhiteSpace(byte b) => b is
        (byte)' ' or //space
        (byte)'\n' or //newline
        (byte)'\r' or //carriage return
        (byte)'\t' or //horizontal tab
        (byte)'\v' or //vertical tab
        (byte)'\f'; //form feed

}