namespace APIDiscovery.Exceptions;

public class BaseException : Exception
{
    public string ErrorCode { get; }
        
    public BaseException(string message) : base(message)
    {
    }
        
    public BaseException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
        
    public BaseException(string message, Exception innerException) : base(message, innerException)
    {
    }
        
    public BaseException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}