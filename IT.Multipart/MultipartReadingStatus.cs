namespace IT.Multipart;

public enum MultipartReadingStatus : byte
{
    Done = 0,
    NotFound = 1,
    NotMappedOrDuplicatedOrOrderWrong = 2
}