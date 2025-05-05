namespace APIDiscovery.Exceptions;

public class BusinessException : BaseException
{
    public BusinessException(string message) : base(message, "BUSINESS_RULE_VIOLATION")
    {
    }
        
    public BusinessException(string message, string errorCode) : base(message, errorCode)
    {
    }
        
    public BusinessException(string message, Exception innerException) 
        : base(message, "BUSINESS_RULE_VIOLATION", innerException)
    {
    }
}