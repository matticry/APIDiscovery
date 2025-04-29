namespace APIDiscovery.Exceptions;

public class InvoiceCalculationException : BusinessException
{
    public InvoiceCalculationException(string message) : base(message, "CALCULATION_ERROR")
    {
    }
        
    public InvoiceCalculationException(string field, decimal expected, decimal actual) 
        : base($"Error de cálculo en el campo '{field}', valor esperado: {expected}, valor actual: {actual}", "CALCULATION_ERROR")
    {
    }
}