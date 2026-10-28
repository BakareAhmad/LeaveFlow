using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Services
{
    public class AuditService(IAuditLogRepository repository, IUnitOfWork unitOfWork) : IAuditService
    {
        public async Task RecordAsync(
            Guid? userId,
            string action,
            string entityType,
            Guid? entityId,
            string? details = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(log);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<BaseResponse<List<AuditLogDto>>> GetRecentAsync()
        {
            var logs = await repository.GetRecentAsync();

            var dtos = new List<AuditLogDto>();
            foreach (var a in logs)
            {
                dtos.Add(new AuditLogDto
                {
                    Id = a.Id,
                    Actor = a.User?.Email ?? "System",
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Details = a.Details,
                    CreatedAt = a.CreatedAt
                });
            }

            return new BaseResponse<List<AuditLogDto>>
            {
                Status = true,
                Data = dtos
            };
        }
    }
}
