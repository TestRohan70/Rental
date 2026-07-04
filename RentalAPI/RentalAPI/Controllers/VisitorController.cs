using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Repository.IRepository;
using RentalAPI.Services;

namespace RentalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitorController : ControllerBase
{
    private readonly IVisitorRepository _visitorRepository;
    private readonly IVisitorPhotoStorageService _photoStorage;

    public VisitorController(
        IVisitorRepository visitorRepository,
        IVisitorPhotoStorageService photoStorage)
    {
        _visitorRepository = visitorRepository;
        _photoStorage = photoStorage;
    }

    [HttpPost]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> Create([FromForm] CreateVisitorRequestFormDto form)
    {
        try
        {
            var photoUrl = await _photoStorage.SaveAsync(form.VisitorPhoto);

            var dto = new CreateVisitorRequestDto
            {
                VisitorName = form.VisitorName,
                VisitorPhone = form.VisitorPhone,
                Purpose = form.Purpose,
                Wing = form.Wing,
                FlatNo = form.FlatNo,
                SecurityId = form.SecurityId,
                VisitorPhotoUrl = photoUrl
            };

            var result = await _visitorRepository.CreateAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("gate/{securityId:int}")]
    public async Task<IActionResult> GetGateRequests(int securityId)
    {
        var data = await _visitorRepository.GetGateRequestsAsync(securityId);
        return Ok(data);
    }

    [HttpGet("resident/{residentId:int}")]
    public async Task<IActionResult> GetResidentRequests(int residentId)
    {
        var data = await _visitorRepository.GetResidentRequestsAsync(residentId);
        return Ok(data);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupResident([FromQuery] string wing, [FromQuery] int flatNo)
    {
        var (data, errorMessage) = await _visitorRepository.LookupResidentAsync(wing, flatNo);
        if (data is null)
        {
            return NotFound(new { message = errorMessage ?? "No approved Tenant or Owner found for this unit." });
        }

        return Ok(data);
    }

    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromQuery] int residentId)
    {
        try
        {
            var result = await _visitorRepository.ApproveAsync(id, residentId);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromQuery] int residentId)
    {
        try
        {
            var result = await _visitorRepository.RejectAsync(id, residentId);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/acknowledge")]
    public async Task<IActionResult> Acknowledge(int id, [FromQuery] int securityId)
    {
        try
        {
            var result = await _visitorRepository.AcknowledgeAsync(id, securityId);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
