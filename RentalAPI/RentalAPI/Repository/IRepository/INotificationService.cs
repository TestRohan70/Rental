using RentalAPI.Models;

namespace RentalAPI.Services
{
    public interface INotificationService
    {
        Task CreateResidentRegistrationNotification(Resident resident);

    }
}   