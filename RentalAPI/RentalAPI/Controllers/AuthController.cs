using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Repository;
using RentalAPI.Repository.IRepository;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IResidentRepository _residentRepo;
    private readonly IAdminRepository _userRepo;
    private readonly JwtService _jwt;

    public AuthController(
        IResidentRepository residentRepo,
        IAdminRepository userRepo,
        JwtService jwt)
    {
        _residentRepo = residentRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    [HttpPost("resident-login")]
    public async Task<IActionResult> ResidentLogin(
        LoginDto dto)
    {
        var resident =
            await _residentRepo.Login(
                dto.UserName,
                dto.Password);

        if (resident == null)
        {
            return Unauthorized(
                "Invalid Credentials");
        }

        var token =
            _jwt.GenerateToken(
                resident.Id,
                resident.Name,
                "Resident");

        return Ok(new
        {
            Token = token,
            Role = "Resident"
        });
    }


    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin(
        LoginDto dto)
    {
        var user =
            await _userRepo.Login(
                dto.UserName,
                dto.Password);

        if (user == null)
        {
            return Unauthorized(
                "Invalid Credentials");
        }

        var token =
            _jwt.GenerateToken(
                user.Id,
                user.UserName,
                "Admin");

        return Ok(new
        {
            Token = token,
            Role = "Admin"
        });
    }
}