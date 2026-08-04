using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Repository;
using RentalAPI.Repository.IRepository;

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

        return Ok(
            "Resident approved successfully.");
    }

    [HttpPut("reject/{id}")]

    public async Task<IActionResult> RejectResident(int id)
    {
        var result = await _adminRepository.RejectResident(id);
        if (!result)
        {
            return NotFound();
        }

        return Ok(
            "Resident rejected successfully.");
    }

    [HttpPost("gate-staff")]
    public async Task<IActionResult> RegisterGateStaff(
        [FromQuery] int adminId,
        [FromBody] RegisterSecurityStaffDto dto)
    {
        try
        {
            var result = await _residentRepository.RegisterSecurityByAdmin(adminId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Email already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("gate-staff")]
    public async Task<IActionResult> GetGateStaff([FromQuery] int adminId)
    {
        if (!await _adminRepository.IsAdmin(adminId))
        {
            return BadRequest(new { message = "Only administrators can view gate security staff." });
        }

        var data = await _residentRepository.GetGateSecurityStaff();
        return Ok(data);
    }
}