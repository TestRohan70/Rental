using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RentalAPI.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IResidentRepository _residentRepository;

    public PublicController(AppDbContext context, IResidentRepository residentRepository)
    {
        _context = context;
        _residentRepository = residentRepository;
    }

    [HttpGet("societies/lookup")]
    public async Task<IActionResult> LookupSociety([FromQuery] string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { message = "Society code is required." });
        }

        var trimmedCode = code.Trim();

        var society = await _context.SocietyMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == trimmedCode);

        if (society == null)
        {
            return NotFound(new { message = "Invalid society code." });
        }

        return Ok(new
        {
            societyId = society.Id,
            code = society.Code,
            name = society.Name
        });
    }

    [HttpGet("societies/{societyId}/wings")]
    public async Task<IActionResult> GetWingsForSociety(int societyId)
    {
        var wings = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Include(c => c.Wing)
            .Where(c => c.SocietyId == societyId && c.IsActive && c.Wing != null)
            .Select(c => c.Wing!)
            .Distinct()
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                id = w.Id,
                code = w.Code,
                name = w.Name
            })
            .ToListAsync();

        return Ok(wings);
    }

    [HttpGet("societies/{societyId}/wings/{wingId}/flats")]
    public async Task<IActionResult> GetFlatsForWing(int societyId, int wingId)
    {
        var configs = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Include(c => c.Wing)
            .Include(c => c.Floor)
            .Include(c => c.Flat)
            .Where(c => c.SocietyId == societyId && c.WingId == wingId && c.IsActive)
            .OrderBy(c => c.Floor != null ? c.Floor.Name : "")
            .ThenBy(c => c.Flat != null ? c.Flat.Code : "")
            .Select(c => new
            {
                societyWingFlatConfigId = c.Id,
                flatId = c.FlatId,
                flatCode = c.Flat != null ? c.Flat.Code : string.Empty,
                floorId = c.FloorId,
                floorName = c.Floor != null ? c.Floor.Name : string.Empty,
                wingId = c.WingId,
                wingName = c.Wing != null ? c.Wing.Name : string.Empty
            })
            .ToListAsync();

        return Ok(configs);
    }

    [HttpPost("resident/signup")]
    public async Task<IActionResult> SelfRegisterResident([FromBody] SelfRegisterResidentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _residentRepository.SelfRegister(dto);
            return StatusCode(201, new { message = "Registration successful. Your account is pending approval." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }
}
