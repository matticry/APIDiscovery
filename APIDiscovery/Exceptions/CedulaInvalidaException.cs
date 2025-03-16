namespace APIDiscovery.Exceptions;

public class CedulaInvalidaException : Exception
{
    public int StatusCode { get; set; }

    public CedulaInvalidaException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}