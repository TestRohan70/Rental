using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Services.IServices;

namespace RentalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PmAdminAccountController : ControllerBase
{
    private readonly IPmAdminAccountService _service;

    public PmAdminAccountController(IPmAdminAccountService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] PmAdminAccountCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("society/{societyId}")]
    public async Task<IActionResult> GetById(int societyId)
    {
        var result = await _service.GetByIdAsync(societyId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }


}