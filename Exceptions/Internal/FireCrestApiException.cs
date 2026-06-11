using System;
using System.Net;

namespace HEAppE.Exceptions.Internal;

public class FirecrestApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseContent { get; }

    public FirecrestApiException(string message, HttpStatusCode statusCode, string responseContent) 
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public FirecrestApiException(string message, HttpStatusCode statusCode, string responseContent, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}