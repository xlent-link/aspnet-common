using System.Net;

namespace XlentLink.AspNet.Common.Exceptions;

public class UnsuccessfulHttpResponseException : Exception
{
    public string Url { get; }
    public HttpStatusCode ResponseStatusCode { get; }
    public string ResponseMessage { get; }

    public UnsuccessfulHttpResponseException(string url, HttpStatusCode responseStatusCode, string responseMessage)
    : base($"Error {responseStatusCode} for request to '{url}'. Message: {responseMessage}")
    {
        Url = url;
        ResponseStatusCode = responseStatusCode;
        ResponseMessage = responseMessage;
    }
}