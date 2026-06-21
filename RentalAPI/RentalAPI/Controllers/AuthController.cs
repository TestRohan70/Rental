using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository;
using RentalAPI.Repository.IRepository;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IResidentRepository _residentRepo;
    private readonly IAdminRepository _userRepo;
    private readonly JwtService _jwt;

    public AuthController(IResidentRepository residentRepo, IAdminRepository userRepo, JwtService jwt)
    {
        _residentRepo = residentRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    [HttpPost("resident-login")]
    public async Task<IActionResult> ResidentLogin(LoginDto dto)

    {
        var resident = await _residentRepo.Login(dto.UserName, dto.Password);

        if (resident == null)
        {
            return Unauthorized("Invalid Credentials");

        }

        var token = _jwt.GenerateToken(resident.Id, resident.Name, "Resident");
        return Ok(new
            {
                Token = token,
                Role = "Resident"
            });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        // 1. Check Admin
        var admin = await _userRepo.Login(dto.UserName, dto.Password);

        if (admin != null)
        {
            var token = _jwt.GenerateToken(admin.Id, admin.UserName, "Admin");

            return Ok(new
            {
                Token = token,
                Role = "Admin",
                UserId = admin.Id,
                UserName = admin.UserName
            });
        }

        // 2. Check Resident
        var resident = await _residentRepo.Login(dto.UserName, dto.Password);

        if (resident != null)
        {
            var token = _jwt.GenerateToken(resident.Id, resident.Name, "Resident");
            return Ok(new
            {
                Token = token,
                Role = "Resident",
                UserId = resident.Id,
                UserName = resident.Name
            });
        }

        // 3. Invalid credentials
        return Unauthorized(new
        {
            Message = "Invalid Username or Password"
        });
    }
}