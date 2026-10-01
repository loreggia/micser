namespace Micser.Engine.Audio;

/// <summary>
/// A request the engine can't fulfill; the API turns it into a problem response with <see cref="StatusCode"/>.
/// </summary>
public sealed class EngineRequestException : Exception
{
    public EngineRequestException(int statusCode, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    /// <summary>
    /// Validation errors by property path, if the request was invalid.
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; }

    public int StatusCode { get; }

    public static EngineRequestException Conflict(string message)
    {
        return new EngineRequestException(StatusCodes.Status409Conflict, message);
    }

    public static EngineRequestException Invalid(string message)
    {
        return new EngineRequestException(StatusCodes.Status400BadRequest, message);
    }

    public static EngineRequestException Invalid(IDictionary<string, string[]> errors)
    {
        return new EngineRequestException(StatusCodes.Status400BadRequest, "The request is invalid.", errors);
    }

    public static EngineRequestException NotFound(string message)
    {
        return new EngineRequestException(StatusCodes.Status404NotFound, message);
    }
}
