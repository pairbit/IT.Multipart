namespace IT.Multipart;

public enum MultipartReadingStatus : sbyte
{
    HeaderFieldValueEndQuoteInvalid = -22,
    HeaderFieldValueEndQuoteNotFound = -21,
    HeaderFieldNameNotFound = -20,
    HeaderNameNotFound = -11,
    HeaderSeparatorNotFound = -10,
    SectionSeparatorNotFound = -5,
    EndBoundaryNotFound = -4,
    BoundaryNotFound = -3,
    StartBoundaryCRLFNotFound = -2,
    StartBoundaryNotFound = -1,
    Done = 0,
    End = 1,
    HeaderNameNotSame = 10,
    HeaderFieldNameNotSame = 20
}