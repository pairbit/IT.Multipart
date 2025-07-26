namespace IT.Multipart;

public enum MultipartReadingStatus : sbyte
{
    HeaderSeparatorNotFound = -11,
    HeaderNameNotFound = -10,
    SectionSeparatorNotFound = -1,
    Done = 0,
    End = 1
}