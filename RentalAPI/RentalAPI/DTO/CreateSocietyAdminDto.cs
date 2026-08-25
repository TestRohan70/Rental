namespace RentalAPI.DTO;

public class CreateSocietyAdminRequestDto
{
    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
}

public class CreateSocietyAdminDto : CreateSocietyAdminRequestDto
{
    public int SocietyId { get; set; }
}
