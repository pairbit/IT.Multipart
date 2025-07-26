namespace IT.Multipart;

public enum MultipartReadingStatus : sbyte
{
    HeaderFieldNameNotFound = -20,
    HeaderSeparatorNotFound = -11,
    HeaderNameNotFound = -10,
    SectionSeparatorNotFound = -1,
    Done = 0,
    End = 1,
    HeaderNameNotSame = 10,
    HeaderFieldNameNotSame = 20
}