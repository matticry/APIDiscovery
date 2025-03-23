namespace APIDiscovery.Models.DTOs;

public class ApiErrorResponse
{
    public string Message { get; set; }
    public double ResponseTimeMs { get; set; }
    public string Fix { get; set; }
}