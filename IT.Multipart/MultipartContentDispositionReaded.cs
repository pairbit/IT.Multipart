namespace IT.Multipart;

public enum MultipartContentDispositionReaded : byte
{
    Done = 0,
    NotFound = 1,
    FieldNotMappedOrDuplicatedOrOrderWrong = 2
}