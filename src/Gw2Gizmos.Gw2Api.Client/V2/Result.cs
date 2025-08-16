using System.Net;

namespace Gw2Gizmos.Gw2Api.Client.V2;

public class Result<TResult, TError>
{
    public TResult? Content { get; }
    public HttpStatusCode StatusCode { get; }
    public TError? Error { get; }
    public int? ResultTotal { get; init; }
    public int? ResultCount { get; init; }
    public Dictionary<string, string[]> ResponseHeaders { get; init; } = new();

    public Result(TResult content, HttpStatusCode statusCode)
    {
        Content = content;
        StatusCode = statusCode;
    }

    public Result(TError error, HttpStatusCode statusCode)
    {
        Error = error;
        StatusCode = statusCode;
    }

    public static implicit operator TResult?(Result<TResult, TError> result) => result.Content;

    public static implicit operator TError?(Result<TResult, TError> result) => result.Error;
}
