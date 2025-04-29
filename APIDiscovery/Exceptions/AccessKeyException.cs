namespace APIDiscovery.Exceptions;

public class AccessKeyException : ElectronicDocumentException
{
    public AccessKeyException(string message) : base(message, "ACCESS_KEY_ERROR")
    {
    }
        
    public AccessKeyException(string generatedKey, string reason) 
        : base($"Error al generar la clave de acceso '{generatedKey}': {reason}", "ACCESS_KEY_ERROR")
    {
    }
}