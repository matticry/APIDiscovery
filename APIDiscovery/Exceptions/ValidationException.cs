namespace APIDiscovery.Exceptions;
public class ValidationException : BaseException
{
    public object InvalidValue { get; }
        
    public ValidationException(string message) : base(message, "VALIDATION_ERROR")
    {
    }
        
    public ValidationException(string message, object invalidValue) : base(message, "VALIDATION_ERROR")
    {
        InvalidValue = invalidValue;
    }
    
    public ValidationException(string message, object invalidValue, Exception innerException) : base(message, "VALIDATION_ERROR", innerException)
    {
        InvalidValue = invalidValue;
    }
        
    public ValidationException(string message, Exception innerException) : base(message, "VALIDATION_ERROR", innerException) { }
}