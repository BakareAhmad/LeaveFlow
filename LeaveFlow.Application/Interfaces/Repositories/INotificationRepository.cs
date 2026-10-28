using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task AddRangeAsync(IEnumerable<Notification> notifications);
        Task<List<Notification>> GetForUserAsync(Guid userId, int take = 100);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<Notification?> GetByIdAsync(Guid id);
        Task<bool> DeduplicationKeyExistsAsync(string key);
        Task MarkAllReadAsync(Guid userId, DateTime readAt);
        void Update(Notification notification);
    }
}
