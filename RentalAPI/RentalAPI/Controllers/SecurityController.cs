using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Repository;
using RentalAPI.Repository.IRepository;

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
            var result = await _alertRepository.CreateAsync(createdById, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
