using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class ForgotPasswordResponseDto
{
    public string Message { get; set; }
    public double ResponseTimeMs { get; set; }
}