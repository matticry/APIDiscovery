namespace APIDiscovery.Models.DTOs;

public class LoginResponseDto
{
    public string Message { get; set; }
    public double ResponseTimeMs { get; set; }
    public string Token { get; set; }
    public UserDto User { get; set; }
}