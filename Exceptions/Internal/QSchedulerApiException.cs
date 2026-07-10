using System;
using System.Net;

namespace HEAppE.Exceptions.Internal;

public class QSchedulerApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseContent { get; }

    public QSchedulerApiException(string message, HttpStatusCode statusCode, string responseContent) 
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public QSchedulerApiException(string message, HttpStatusCode statusCode, string responseContent, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}
