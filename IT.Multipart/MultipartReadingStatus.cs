namespace IT.Multipart;

public enum MultipartReadingStatus : sbyte
{
    SectionSeparatorNotFound = -1,
    Done = 0,
    End = 1
}