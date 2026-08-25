using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Constants;
using RentalAPI.DTO;
using RentalAPI.Repository;
using RentalAPI.Repository.IRepository;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RentalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminRepository _adminRepository;
    private readonly IResidentRepository _residentRepository;

    public AdminController(IAdminRepository adminRepository, IResidentRepository residentRepository)
    {
        _adminRepository = adminRepository;
        _residentRepository = residentRepository;
    }

    [HttpGet("pendingResidents")]
    public async Task<IActionResult> GetPendingResidents()
    {
        var residents = await _adminRepository.GetPendingResidents();
        return Ok(residents);
    }

    [HttpPut("approve/{id}")]
    public async Task<IActionResult> ApproveResident(int id)
    {
        var result = await _adminRepository.ApproveResident(id);
        if (!result)
        {
            return NotFound();
        }

        return Ok("Resident approved successfully.");
    }

    [HttpPut("reject/{id}")]
    public async Task<IActionResult> RejectResident(int id)
    {
        var result = await _adminRepository.RejectResident(id);
        if (!result)
        {
            return NotFound();
        }

        return Ok("Resident rejected successfully.");
    }

    [HttpPost("resident")]
    public async Task<IActionResult> RegisterResidentByAdmin(
        [FromQuery] int? adminUserId,
        [FromBody] CreateResidentUserDto dto)
    {
        try
        {
            int resolvedAdminId = GetAuthenticatedUserId(adminUserId);
            var result = await _residentRepository.RegisterResidentByAdmin(resolvedAdminId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("gate-staff")]
    public async Task<IActionResult> RegisterGateStaff(
        [FromQuery] int? adminId,
        [FromBody] CreateSecurityUserDto dto)
    {
        try
        {
            int resolvedAdminId = GetAuthenticatedUserId(adminId);
            var result = await _adminRepository.RegisterSecurityStaff(resolvedAdminId, dto);
            return Ok(new
            {
                message = "Gate security staff created successfully.",
                result.Id,
                result.UserName,
                result.Email
            });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("gate-staff")]
    public async Task<IActionResult> GetGateStaff([FromQuery] int? adminId)
    {
        int resolvedAdminId = GetAuthenticatedUserId(adminId);
        if (!await _adminRepository.IsAdmin(resolvedAdminId))
        {
            return BadRequest(new { message = "Only administrators can view gate security staff." });
        }

        var data = await _adminRepository.GetGateSecurityStaff(resolvedAdminId);
        return Ok(data);
    }

    private int GetAuthenticatedUserId(int? fallbackId)
    {
        var claimValue = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimValue, out int userId) && userId > 0)
        {
            return userId;
        }

        return fallbackId ?? 0;
    }
}