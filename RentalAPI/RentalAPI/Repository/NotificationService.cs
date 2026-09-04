using Microsoft.EntityFrameworkCore;
using RentalAPI.Constants;
using RentalAPI.Models;
using System;
using System.Collections.Generic;
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

        public async Task CreateUnplannedVisitorNotification(VisitorRequest request)
        {
            var resident = await _context.Residents
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.ResidentId);

            if (resident?.UserId != null)
            {
                await _context.Notifications.AddAsync(new Notification
                {
                    UserId = resident.UserId.Value,
                    ResidentId = resident.Id,
                    Title = "New Visitor Request",
                    Message = $"Visitor {request.VisitorName} is waiting for your approval at the gate for Wing {request.Wing}, Flat {request.FlatNo}.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateUnplannedVisitorApprovedNotification(VisitorRequest request)
        {
            if (request.SecurityUserId.HasValue)
            {
                await _context.Notifications.AddAsync(new Notification
                {
                    UserId = request.SecurityUserId.Value,
                    ResidentId = request.ResidentId,
                    Title = "Visitor Request Approved",
                    Message = $"Resident approved visitor {request.VisitorName} for Wing {request.Wing}, Flat {request.FlatNo}.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateUnplannedVisitorRejectedNotification(VisitorRequest request)
        {
            if (request.SecurityUserId.HasValue)
            {
                await _context.Notifications.AddAsync(new Notification
                {
                    UserId = request.SecurityUserId.Value,
                    ResidentId = request.ResidentId,
                    Title = "Visitor Request Rejected",
                    Message = $"Resident rejected the visitor request for {request.VisitorName} (Wing {request.Wing}, Flat {request.FlatNo}).",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreatePlannedVisitorCreatedNotification(VisitorRequest request)
        {
            await Task.CompletedTask;
        }

        public async Task CreateVisitorCheckedInNotification(VisitorRequest request)
        {
            var resident = await _context.Residents
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.ResidentId);

            if (resident?.UserId != null)
            {
                await _context.Notifications.AddAsync(new Notification
                {
                    UserId = resident.UserId.Value,
                    ResidentId = resident.Id,
                    Title = "Visitor Checked In",
                    Message = $"Visitor {request.VisitorName} has checked in at the gate.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateVisitorCheckedOutNotification(VisitorRequest request)
        {
            var resident = await _context.Residents
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.ResidentId);

            if (resident?.UserId != null)
            {
                await _context.Notifications.AddAsync(new Notification
                {
                    UserId = resident.UserId.Value,
                    ResidentId = resident.Id,
                    Title = "Visitor Checked Out",
                    Message = $"Visitor {request.VisitorName} has checked out.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}