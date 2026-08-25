using Microsoft.EntityFrameworkCore;
using RentalAPI.Constants;
using RentalAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            var admins = await _context.SysmUsers
                .Include(u => u.RoleNavigation)
                .Where(x => x.RoleNavigation != null &&
                            (x.RoleNavigation.Code == AppRoles.SuperAdmin || x.RoleNavigation.Code == AppRoles.SocietyAdmin))
                .ToListAsync();

            if (!admins.Any())
            {
                return;
            }

            var notifications = admins.Select(admin => new Notification
            {
                UserId = admin.Id,
                ResidentId = resident.Id,
                Title = "New Resident Request",
                Message = $"{resident.Name} requested approval.",
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            });

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }
    }
}