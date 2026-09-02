using System;
using Microsoft.AspNetCore.Http;

namespace RentalAPI.DTO;

public class CreatePlannedVisitorRequestDto
{
    public string VisitorName { get; set; } = null!;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    public DateTime? ExpectedArrivalDateTime { get; set; }
}

public class WingDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class FloorDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int FloorNumber { get; set; }
}

public class FlatDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
}

public class CreateUnplannedVisitorRequestDto
{
    public string VisitorName { get; set; } = null!;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    public int SocietyWingFlatConfigId { get; set; }
}

public class CreateUnplannedVisitorFormDto
{
    public string VisitorName { get; set; } = null!;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    public int SocietyWingFlatConfigId { get; set; }

    public IFormFile? VisitorPhoto { get; set; }
}

public class PlannedVisitorResponseDto
{
    public string Message { get; set; } = "Visitor pass created successfully";

    public int VisitorRequestId { get; set; }

    public string Otp { get; set; } = null!;

    public DateTime OtpExpiresAt { get; set; }
}

public class VerifyOtpDto
{
    public int VisitorRequestId { get; set; }

    public string Otp { get; set; } = null!;
}

public class CheckInDto
{
    public int VisitorRequestId { get; set; }

    public string? Gate { get; set; }
}

public class CheckOutDto
{
    public int VisitorRequestId { get; set; }
}

public class VisitorRequestResponseDto
{
    public int Id { get; set; }

    public string VisitorName { get; set; } = null!;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    public string? Wing { get; set; }

    public string? FlatNo { get; set; }

    public int ResidentId { get; set; }

    public string? ResidentName { get; set; }

    public int? SecurityUserId { get; set; }

    public string? SecurityUserName { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? RespondedDate { get; set; }

    public DateTime? AcknowledgedDate { get; set; }

    public string? VisitorPhotoUrl { get; set; }

    public int? SocietyId { get; set; }

    public string? SocietyName { get; set; }

    public int? SocietyWingFlatConfigId { get; set; }

    public string? VisitType { get; set; }

    public DateTime? ExpectedArrivalDateTime { get; set; }

    public DateTime? OTPExpiresAt { get; set; }

    public DateTime? OTPVerifiedAt { get; set; }

    public int? StatusId { get; set; }

    public string? StatusCode { get; set; }

    public string? StatusName { get; set; }

    public DateTime? CheckInDateTime { get; set; }

    public DateTime? CheckOutDateTime { get; set; }
}

public class VisitorVisitResponseDto
{
    public int Id { get; set; }

    public int VisitorRequestId { get; set; }

    public string VisitorName { get; set; } = null!;

    public string? Wing { get; set; }

    public string? FlatNo { get; set; }

    public DateTime? CheckInDateTime { get; set; }

    public DateTime? CheckOutDateTime { get; set; }

    public int? CheckInSecurityUserId { get; set; }

    public string? CheckInSecurityUserName { get; set; }

    public int? CheckOutSecurityUserId { get; set; }

    public string? CheckOutSecurityUserName { get; set; }

    public string? Gate { get; set; }

    public int StatusId { get; set; }

    public string? StatusCode { get; set; }

    public string? StatusName { get; set; }

    public DateTime CreatedDate { get; set; }
}
