using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Repository.IRepository;
using System;
using System.Threading.Tasks;

namespace RentalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly ISocietyAlertRepository _alertRepository;

    public SecurityController(ISocietyAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    [HttpPost("alerts")]
    public async Task<IActionResult> CreateAlert(
        [FromQuery] int createdById,
        [FromBody] CreateSocietyAlertDto dto)
    {
        try
        {
            var result = await _alertRepository.CreateAsync(createdBySecurityId: createdById, dto: dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] int createdById)
    {
        try
        {
            var data = await _alertRepository.GetBySecurityIdAsync(createdById);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
