namespace IT.Multipart;

public enum MultipartReadingStatus : sbyte
{
    HeaderFieldValueEndQuoteInvalid = -22,
    HeaderFieldValueEndQuoteNotFound = -21,
    HeaderFieldNameNotFound = -20,
    HeaderNameNotFound = -12,
    HeaderSeparatorNotFound = -11,
    HeadersNotFound = -10,
    SectionSeparatorNotFound = -6,
    EndBoundaryNotFound = -5,
    BoundaryNotFound = -4,
    StartBoundaryCRLFNotFound = -3,
    StartBoundaryNotFound = -2,
    SectionsNotFound = -1,
    Done = 0,
    End = 1,
    HeaderNameNotSame = 10,
    HeaderFieldNameNotSame = 20
}