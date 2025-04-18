﻿using APIDiscovery.Models.DTOs;
using APIDiscovery.Services;
using APIDiscovery.Services.Security;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = APIDiscovery.Models.DTOs.LoginRequest;

namespace APIDiscovery.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly RabbitMQService _rabbitMqService;

    public AuthController(AuthService authService, RabbitMQService rabbitMqService)
    {
        _authService = authService;
        _rabbitMqService = rabbitMqService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var token = await _authService.Authenticate(loginRequest.Email, loginRequest.Password);
        _rabbitMqService.PublishUserAction(new UserActionEvent { Username = loginRequest.Email, Action = "login" });
        return Ok(new { token });
    }
    
    [HttpPost("login-with-role")]
    public async Task<IActionResult> LoginWithRole([FromBody] LoginWithRoleRequest loginRequest)
    {
        var response = await _authService.LoginWithRole(
            loginRequest.Cedula, 
            loginRequest.Password, 
            loginRequest.Role
        );
    
        _rabbitMqService.PublishUserAction(new UserActionEvent 
        { 
            Username = loginRequest.Cedula, 
            Action = "login-with-role",
            Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            
        });
    
        return Ok(response);
    }
    
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var response = await _authService.ForgotPassword(request.Email);
    
        _rabbitMqService.PublishUserAction(new UserActionEvent 
        { 
            Username = request.Email, 
            Action = "forgot-password-request"
        });
    
        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var response = await _authService.VerifyCodeAndResetPassword(
            request.Email,
            request.Code,
            request.NewPassword
        );
    
        _rabbitMqService.PublishUserAction(new UserActionEvent 
        { 
            Username = request.Email, 
            Action = "password-reset-successful"
        });
    
        return Ok(response);
    }
    

}