using Microsoft.AspNetCore.Mvc;
using RentalAPI.Repository.IRepository;

[ApiController]

[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminRepository _adminRepository;


    public AdminController(IAdminRepository adminRepository)

    {
        _adminRepository = adminRepository;

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
}