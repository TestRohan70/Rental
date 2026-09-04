using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.Constants;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository;

namespace RentalAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext context, JwtService jwt)
    {
        _context = context;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { Message = "UserName/Email and Password are required." });
        }

        var input = dto.UserName.Trim();

        var user = await _context.SysmUsers
            .Include(u => u.RoleNavigation)
            .Include(u => u.Resident)
            .FirstOrDefaultAsync(u =>
                (u.UserName != null && u.UserName.ToLower() == input.ToLower()) ||
                (u.Email != null && u.Email.ToLower() == input.ToLower()));

        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid Username or Password." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { Message = "User account is inactive." });
        }

        bool isValidPassword = false;
        if (!string.IsNullOrEmpty(user.Password))
        {
            if (user.Password.StartsWith("$2"))
            {
                try
                {
                    isValidPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
                }
                catch { }
            }

            if (!isValidPassword && user.Password == dto.Password)
            {
                isValidPassword = true;
            }
        }

        if (!isValidPassword)
        {
            return Unauthorized(new { Message = "Invalid Username or Password." });
        }

        var roleCode = user.RoleNavigation?.Code ?? AppRoles.Resident;

        int? societyId = null;
        if (roleCode == AppRoles.SocietyAdmin || roleCode == AppRoles.Security)
        {
            var userMapping = await _context.SocietyUserMappings
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.IsActive);
            societyId = userMapping?.SocietyId;
        }

        if (roleCode == "RESIDENT")
        {
            if (user.Resident == null)
            {
                return Unauthorized(new
                {
                    Message = "Resident profile not found."
                });
            }

            if (user.Resident.Status == "Pending")
            {
                return Unauthorized(new
                {
                    Message = "Your account is pending For approval."
                });
            }
        }

        int? residentId = user.Resident?.Id;

        var token = _jwt.GenerateToken(user, roleCode, societyId);

        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            UserName = user.UserName ?? user.Email,
            Email = user.Email,
            RoleId = user.RoleId,
            Role = roleCode,
            SocietyId = societyId,
            ResidentId = residentId
        });
    }

    [HttpPost("resident-login")]
    public async Task<IActionResult> ResidentLogin([FromBody] LoginDto dto)
    {
        return await Login(dto);
    }
}