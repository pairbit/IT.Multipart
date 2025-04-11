namespace IT.Multipart;

//System.Net.Http.Headers.ContentDispositionHeaderValue
//RFC 5987 encoding
//charset "'" [ language ] "'" value-chars
//title*=iso-8859-1'en'%A3%20rates
//title*=UTF-8''%c2%a3%20and%20%e2%82%ac%20rates
//title*=utf-8''%e2%82%ac%20exchange%20rates
internal ref struct MultipartHeaderFieldValueReader
{
}