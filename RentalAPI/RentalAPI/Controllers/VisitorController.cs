using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Repository.IRepository;
using RentalAPI.Services;
using System.Security.Claims;

namespace RentalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out int userId))
        {
            return userId;
        }
        throw new InvalidOperationException("User identity claim missing or invalid.");
    }

    [HttpPost("planned")]
    public async Task<IActionResult> CreatePlanned([FromBody] CreatePlannedVisitorRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _visitorRepository.CreatePlannedAsync(userId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("unplanned")]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> CreateUnplanned([FromForm] CreateUnplannedVisitorFormDto form)
    {
        try
        {
            var userId = GetCurrentUserId();
            string? photoUrl = null;

            if (form.VisitorPhoto != null)
            {
                photoUrl = await _photoStorage.SaveAsync(form.VisitorPhoto);
            }

            var dto = new CreateUnplannedVisitorRequestDto
            {
                VisitorName = form.VisitorName,
                VisitorPhone = form.VisitorPhone,
                Purpose = form.Purpose,
                SocietyWingFlatConfigId = form.SocietyWingFlatConfigId
            };

            var result = await _visitorRepository.CreateUnplannedAsync(userId, dto, photoUrl);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("resident")]
    public async Task<IActionResult> GetResidentRequests()
    {
        var userId = GetCurrentUserId();
        var data = await _visitorRepository.GetResidentRequestsAsync(userId);
        return Ok(data);
    }

    [HttpGet("resident/{residentId:int}")]
    public async Task<IActionResult> GetResidentRequestsLegacy(int residentId)
    {
        var userId = GetCurrentUserId();
        var data = await _visitorRepository.GetResidentRequestsAsync(userId);
        return Ok(data);
    }

    [HttpPost("{id:int}/approve")]
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _visitorRepository.ApproveUnplannedAsync(userId, id);
            if (result is null)
            {
                return NotFound(new { message = "Visitor request not found." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reject")]
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _visitorRepository.RejectUnplannedAsync(userId, id);
            if (result is null)
            {
                return NotFound(new { message = "Visitor request not found." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _visitorRepository.CancelPlannedAsync(userId, id);
            if (result is null)
            {
                return NotFound(new { message = "Visitor request not found." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("gate")]
    public async Task<IActionResult> GetGateRequests()
    {
        var userId = GetCurrentUserId();
        var data = await _visitorRepository.GetGateRequestsAsync(userId);
        return Ok(data);
    }

    [HttpGet("gate/{securityId:int}")]
    public async Task<IActionResult> GetGateRequestsLegacy(int securityId)
    {
        var userId = GetCurrentUserId();
        var data = await _visitorRepository.GetGateRequestsAsync(userId);
        return Ok(data);
    }

    [HttpGet("gate/{securityId:int}/history")]
    public async Task<IActionResult> GetGateRequestHistoryLegacy(int securityId)
    {
        var userId = GetCurrentUserId();
        var data = await _visitorRepository.GetSocietyHistoryAsync(userId);
        return Ok(data);
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var verified = await _visitorRepository.VerifyOtpAsync(userId, dto);
            return Ok(new { success = verified, message = "OTP verified successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/check-in")]
    public async Task<IActionResult> CheckIn(int id, [FromBody] CheckInDto? dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var checkInDto = dto ?? new CheckInDto { VisitorRequestId = id };
            checkInDto.VisitorRequestId = id;

            var result = await _visitorRepository.CheckInAsync(userId, checkInDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/check-out")]
    public async Task<IActionResult> CheckOut(int id, [FromBody] CheckOutDto? dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var checkOutDto = dto ?? new CheckOutDto { VisitorRequestId = id };
            checkOutDto.VisitorRequestId = id;

            var result = await _visitorRepository.CheckOutAsync(userId, checkOutDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("inside")]
    public async Task<IActionResult> GetCurrentlyInside()
    {
        var userId = GetCurrentUserId();
        var data = await _visitorRepository.GetCurrentlyInsideAsync(userId);
        return Ok(data);
    }

    [HttpGet("society/history")]
    public async Task<IActionResult> GetSocietyHistory()
    {
        var userId = GetCurrentUserId();
        var data = await _visitorRepository.GetSocietyHistoryAsync(userId);
        return Ok(data);
    }

    [HttpGet("wings")]
    public async Task<IActionResult> GetWings()
    {
        var currentUserId = GetCurrentUserId();
        var wings = await _visitorRepository.GetSocietyWingsAsync(currentUserId);
        return Ok(wings);
    }

    [HttpGet("floors")]
    public async Task<IActionResult> GetFloors([FromQuery] int wingId)
    {
        var currentUserId = GetCurrentUserId();
        var floors = await _visitorRepository.GetSocietyFloorsAsync(currentUserId, wingId);
        return Ok(floors);
    }

    [HttpGet("flats")]
    public async Task<IActionResult> GetFlats([FromQuery] int wingId, [FromQuery] int floorId)
    {
        var currentUserId = GetCurrentUserId();
        var flats = await _visitorRepository.GetSocietyFlatsAsync(currentUserId, wingId, floorId);
        return Ok(flats);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupResident(
        [FromQuery] int? wingId,
        [FromQuery] int? floorId,
        [FromQuery] int? flatId,
        [FromQuery] string? wing,
        [FromQuery] int? flatNo)
    {
        var currentUserId = GetCurrentUserId();

        if (wingId.HasValue && floorId.HasValue && flatId.HasValue)
        {
            var (idData, idError) = await _visitorRepository.LookupResidentByFlatConfigAsync(currentUserId, wingId.Value, floorId.Value, flatId.Value);
            if (idData is null)
            {
                return NotFound(new { message = idError ?? "No approved resident found for this unit." });
            }
            return Ok(idData);
        }

        var (data, errorMessage) = await _visitorRepository.LookupResidentAsync(currentUserId, wing, flatNo);
        if (data is null)
        {
            return NotFound(new { message = errorMessage ?? "No approved resident found for this unit." });
        }

        return Ok(data);
    }
}
