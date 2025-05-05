using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmissionPointsController : ControllerBase
{
    private readonly IEmissionPointService _emissionPointService;

    public EmissionPointsController(IEmissionPointService emissionPointService)
    {
        _emissionPointService = emissionPointService;
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<List<EmissionPointWithSequenceDto>>> GetByBranch(int branchId)
    {
        var emissionPoints = await _emissionPointService.GetEmissionPointsWithSequencesByBranchId(branchId);
        if (emissionPoints == null || emissionPoints.Count == 0)
            return NotFound($"No se encontraron puntos de emisión para la sucursal {branchId}");

        return Ok(emissionPoints);
    }
    

}