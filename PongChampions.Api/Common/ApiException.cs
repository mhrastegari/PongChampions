namespace PongChampions.Api.Common;

public sealed class ApiException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ApiException NotFound(string message)
    {
        return new(404, message);
    }

    public static ApiException Conflict(string message)
    {
        return new(409, message);
    }

    public static ApiException Forbidden(string message)
    {
        return new(403, message);
    }

    public static ApiException Invalid(string message)
    {
        return new(400, message);
    }
}
