namespace APIDiscovery.Exceptions;

public class ClientDocumentException : ValidationException
{
    public string DocumentType { get; }
    public string DocumentNumber { get; }
        
    public ClientDocumentException(string message) : base(message)
    {
    }
        
    public ClientDocumentException(string documentType, string documentNumber, string reason) 
        : base($"Documento '{documentType}' con número '{documentNumber}' inválido: {reason}")
    {
        DocumentType = documentType;
        DocumentNumber = documentNumber;
    }
}