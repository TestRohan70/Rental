using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Constants;
using RentalAPI.DTO.PAdmin;
using RentalAPI.Repository.IRepository;

namespace RentalAPI.Controllers;

[ApiController]
[Route("api/padmin")]
[Authorize(Roles = AppRoles.PAdmin)]
public class PAdminController : ControllerBase
{
    private readonly ISocietyConfigurationRepository _repository;

    public PAdminController(ISocietyConfigurationRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("masters/wings")]
    public async Task<IActionResult> GetWings(CancellationToken cancellationToken)
    {
        var data = await _repository.GetActiveWingsAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("masters/floors")]
    public async Task<IActionResult> GetFloors([FromQuery] int? wingId, CancellationToken cancellationToken)
    {
        var data = await _repository.GetActiveFloorsAsync(wingId, cancellationToken);
        return Ok(data);
    }

    [HttpGet("masters/flats")]
    public async Task<IActionResult> GetFlats(CancellationToken cancellationToken)
    {
        var data = await _repository.GetActiveFlatsAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("societies")]
    public async Task<IActionResult> GetSocieties([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var data = await _repository.GetSocietiesAsync(search, cancellationToken);
        return Ok(data);
    }

    [HttpGet("societies/{id:int}")]
    public async Task<IActionResult> GetSociety(int id, CancellationToken cancellationToken)
    {
        var data = await _repository.GetSocietyAsync(id, cancellationToken);
        return data is null ? NotFound(new { message = "Society not found." }) : Ok(data);
    }

    [HttpPost("societies")]
    public async Task<IActionResult> CreateSociety([FromBody] CreateSocietyDto dto, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _repository.CreateSocietyAsync(dto, cancellationToken), nameof(GetSociety));
    }

    [HttpPut("societies/{id:int}")]
    public async Task<IActionResult> UpdateSociety(int id, [FromBody] UpdateSocietyDto dto, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _repository.UpdateSocietyAsync(id, dto, cancellationToken);
            return result ?? throw new KeyNotFoundException("Society not found.");
        });
    }

    [HttpDelete("societies/{id:int}")]
    public async Task<IActionResult> DeleteSociety(int id, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _repository.DeleteSocietyAsync(id, cancellationToken);
            return Ok(new { message = "Society deleted successfully." });
        });
    }

    [HttpGet("societies/{societyId:int}/structure")]
    public async Task<IActionResult> GetStructure(int societyId, CancellationToken cancellationToken)
    {
        var data = await _repository.GetSocietyStructureAsync(societyId, cancellationToken);
        return data is null ? NotFound(new { message = "Society not found." }) : Ok(data);
    }

    [HttpPost("societies/{societyId:int}/mappings")]
    public async Task<IActionResult> AddMapping(int societyId, [FromBody] CreateSocietyMappingDto dto, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _repository.AddMappingAsync(societyId, dto, cancellationToken));
    }

    [HttpPost("societies/{societyId:int}/generate-structure")]
    public async Task<IActionResult> GenerateStructure(int societyId, [FromBody] GenerateSocietyStructureDto dto, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _repository.GenerateStructureAsync(societyId, dto, cancellationToken));
    }

    [HttpDelete("societies/{societyId:int}/wings/{wingId:int}")]
    public async Task<IActionResult> DeactivateWing(int societyId, int wingId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _repository.DeactivateWingForSocietyAsync(societyId, wingId, cancellationToken);
            return Ok(new { message = "Wing configuration deactivated." });
        });
    }

    [HttpDelete("societies/{societyId:int}/wings/{wingId:int}/floors/{floorId:int}")]
    public async Task<IActionResult> DeactivateFloor(int societyId, int wingId, int floorId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _repository.DeactivateFloorForSocietyAsync(societyId, wingId, floorId, cancellationToken);
            return Ok(new { message = "Floor configuration deactivated." });
        });
    }

    [HttpDelete("societies/{societyId:int}/wings/{wingId:int}/floors/{floorId:int}/flats/{flatId:int}")]
    public async Task<IActionResult> DeactivateFlat(int societyId, int wingId, int floorId, int flatId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _repository.DeactivateFlatAsync(societyId, wingId, floorId, flatId, cancellationToken);
            return Ok(new { message = "Flat deactivated." });
        });
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action, string? createdAction = null)
    {
        try
        {
            var result = await action();
            if (createdAction is not null && result is SocietyResponseDto created)
            {
                return CreatedAtAction(createdAction, new { id = created.Id }, created);
            }

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            var message = ex.Message;
            if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("already mapped", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message });
            }

            return BadRequest(new { message });
        }
    }
}
