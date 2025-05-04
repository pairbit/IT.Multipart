namespace IT.Multipart;

public enum MultipartContentDispositionReaded : byte
{
    Done = 0,
    TypeNotFound = 1,
    FieldNotMappedOrDuplicatedOrOrderWrong = 2
}