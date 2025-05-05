namespace APIDiscovery.Exceptions;

public class SequenceException : BusinessException
{
    public SequenceException(string message) : base(message, "SEQUENCE_ERROR")
    {
    }
        
    public SequenceException(string sequenceCode, string reason) 
        : base($"Error con la secuencia '{sequenceCode}': {reason}", "SEQUENCE_ERROR")
    {
    }
}