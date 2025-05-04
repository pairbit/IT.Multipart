namespace IT.Multipart;

public enum MultipartSectionWithContentDispositionReaded : byte
{
    Done = 0,
    SectionNotFound = 1,
    ContentDispositionNotFound = 2,
    ContentDispositionTypeNotFound = 3,
    ContentDispositionFieldNotMappedOrDuplicatedOrOrderWrong = 4
}