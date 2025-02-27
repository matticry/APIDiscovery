using APIDiscovery.Models.DTOs;
using APIDiscovery.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var token = await _authService.Authenticate(loginRequest.Email, loginRequest.Password);
        return Ok(new { token });
    }
}