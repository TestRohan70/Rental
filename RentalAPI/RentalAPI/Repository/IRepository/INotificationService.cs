using System.Threading.Tasks;
using RentalAPI.Models;

namespace RentalAPI.Services
{
    public interface INotificationService
    {
        Task CreateResidentRegistrationNotification(Resident resident);
        Task CreateUnplannedVisitorNotification(VisitorRequest request);
        Task CreateUnplannedVisitorApprovedNotification(VisitorRequest request);
        Task CreateUnplannedVisitorRejectedNotification(VisitorRequest request);
        Task CreatePlannedVisitorCreatedNotification(VisitorRequest request);
        Task CreateVisitorCheckedInNotification(VisitorRequest request);
        Task CreateVisitorCheckedOutNotification(VisitorRequest request);
    }
}