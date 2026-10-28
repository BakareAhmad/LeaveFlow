using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class NotificationRepository(LeaveFlowDbContext context) : INotificationRepository
    {
        public async Task AddAsync(Notification notification)
        {
            await context.Notifications.AddAsync(notification);
        }

        public async Task AddRangeAsync(IEnumerable<Notification> notifications)
        {
            await context.Notifications.AddRangeAsync(notifications);
        }

        public async Task<List<Notification>> GetForUserAsync(Guid userId, int take = 100)
        {
            return await context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        }

        public async Task<bool> DeduplicationKeyExistsAsync(string key)
        {
            return await context.Notifications
                .AsNoTracking()
                .AnyAsync(n => n.DeduplicationKey == key && !n.IsDeleted);
        }

        public async Task MarkAllReadAsync(Guid userId, DateTime readAt)
        {
            await context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(n => n.IsRead, true)
                     .SetProperty(n => n.ReadAt, readAt));
        }

        public void Update(Notification notification)
        {
            context.Entry(notification).State = EntityState.Modified;
        }
    }
}
