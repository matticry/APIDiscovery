namespace APIDiscovery.Exceptions;

public class ElectronicDocumentException : BaseException
{
    public ElectronicDocumentException(string message) : base(message, "ELECTRONIC_DOCUMENT_ERROR")
    {
    }
        
    public ElectronicDocumentException(string message, string errorCode) : base(message, errorCode)
    {
    }
        
    public ElectronicDocumentException(string message, Exception innerException) 
        : base(message, "ELECTRONIC_DOCUMENT_ERROR", innerException)
    {
    
    }
}
