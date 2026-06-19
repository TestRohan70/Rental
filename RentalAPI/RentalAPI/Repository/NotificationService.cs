using Microsoft.EntityFrameworkCore;

using RentalAPI.Models;

namespace RentalAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext context, ILogger<NotificationService> logger)

        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateResidentRegistrationNotification(Resident resident)
        {
            

            var admins = await _context.SysmUsers.Where(x => x.Role == "Admin").ToListAsync();
            if (!admins.Any())
            {
                return;
            }

            // Notifications banao

            var notifications = admins.Select(admin => new Notification
            {
                UserId = admin.Id,
                ResidentId = resident.Id,
                Title = "New Resident Request",
                Message = $"{resident.Name} " + $"({resident.Wing}-{resident.FlatNo}) " + $"requested approval.",
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            });

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();



        }
    }
}