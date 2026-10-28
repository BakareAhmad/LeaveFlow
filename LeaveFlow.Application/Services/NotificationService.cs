using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Services
{
    public class NotificationService(INotificationRepository repository,
        IUnitOfWork unitOfWork,
        IRealTimeNotifier notifier) : INotificationService
    {
        private static NotificationDto MapToDto(Notification n)
        {
            return new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                RelatedUrl = n.RelatedUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            };
        }

        public async Task CreateAsync(
            Guid userId,
            string title,
            string message,
            NotificationType type,
            string? relatedUrl = null,
            string? deduplicationKey = null)
        {
            if (!string.IsNullOrWhiteSpace(deduplicationKey))
            {
                var alreadyExists = await repository.DeduplicationKeyExistsAsync(deduplicationKey);
                if (alreadyExists)
                {
                    return;
                }
            }

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedUrl = relatedUrl,
                DeduplicationKey = deduplicationKey
            };

            await repository.AddAsync(notification);
            await unitOfWork.SaveChangesAsync();
            await notifier.NotifyUserAsync(userId, MapToDto(notification));
        }

        public async Task CreateManyAsync(
            IEnumerable<Guid> userIds,
            string title,
            string message,
            NotificationType type,
            string? relatedUrl = null)
        {
            var notifications = userIds
                .Distinct()
                .Select(id => new Notification
                {
                    UserId = id,
                    Title = title,
                    Message = message,
                    Type = type,
                    RelatedUrl = relatedUrl
                })
                .ToList();

            await repository.AddRangeAsync(notifications);
            await unitOfWork.SaveChangesAsync();

            foreach (var item in notifications)
            {
                await notifier.NotifyUserAsync(item.UserId, MapToDto(item));
            }
        }

        public async Task<BaseResponse<List<NotificationDto>>> GetForUserAsync(Guid userId)
        {
            var items = await repository.GetForUserAsync(userId);
            var dtos = items.Select(n => MapToDto(n)).ToList();

            return new BaseResponse<List<NotificationDto>>
            {
                Status = true,
                Data = dtos
            };
        }

        public async Task<BaseResponse<int>> GetUnreadCountAsync(Guid userId)
        {
            var count = await repository.GetUnreadCountAsync(userId);
            return new BaseResponse<int>
            {
                Status = true,
                Data = count
            };
        }

        public async Task<BaseResponse<bool>> MarkReadAsync(Guid id, Guid userId)
        {
            var item = await repository.GetByIdAsync(id);
            if (item == null || item.UserId != userId)
            {
                return new BaseResponse<bool> { Message = "Notification not found." };
            }

            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
            repository.Update(item);
            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Status = true,
                Data = true
            };
        }

        public async Task<BaseResponse<bool>> MarkAllReadAsync(Guid userId)
        {
            await repository.MarkAllReadAsync(userId, DateTime.UtcNow);
            return new BaseResponse<bool>
            {
                Status = true,
                Data = true
            };
        }
    }
}
