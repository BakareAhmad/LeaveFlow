using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task CreateAsync(Guid userId, string title, string message, NotificationType type, string? relatedUrl = null, string? deduplicationKey = null);
        Task CreateManyAsync(IEnumerable<Guid> userIds, string title, string message, NotificationType type, string? relatedUrl = null);
        Task<BaseResponse<List<NotificationDto>>> GetForUserAsync(Guid userId);
        Task<BaseResponse<int>> GetUnreadCountAsync(Guid userId);
        Task<BaseResponse<bool>> MarkReadAsync(Guid id, Guid userId);
        Task<BaseResponse<bool>> MarkAllReadAsync(Guid userId);
    }

    public interface IRealTimeNotifier
    {
        Task NotifyUserAsync(Guid userId, NotificationDto notification);
    }
}
