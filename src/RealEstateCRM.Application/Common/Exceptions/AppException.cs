namespace RealEstateCRM.Application.Common.Exceptions;

/// <summary>
/// Exception carrying an HTTP status code, translated to a ProblemDetails response at the API boundary.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
