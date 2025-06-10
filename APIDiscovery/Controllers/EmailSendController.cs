using APIDiscovery.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EmailSendController : ControllerBase
{
    private readonly IEmailSendService _emailSendService;
    public EmailSendController(IEmailSendService emailSendService)
    {
        _emailSendService = emailSendService;
    }
    
    [HttpGet ("SendInvoiceEmail/{invoiceId}")]
    public async Task<IActionResult> SendInvoiceEmail(int invoiceId)
    {
        try
        {
            var result = await _emailSendService.SendInvoiceEmailAsync(invoiceId);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
}