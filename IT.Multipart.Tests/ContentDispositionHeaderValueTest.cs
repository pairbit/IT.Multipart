using System.Net.Http.Headers;

namespace IT.Multipart.Tests;

internal class ContentDispositionHeaderValueTest
{
    [Test]
    public void Test()
    {
        var fileName = "имя файла.pdf";

        var header = new ContentDispositionHeaderValue("form-data");
        header.Name = "name";
        header.FileName = fileName;
        header.FileNameStar = fileName;


    }
}