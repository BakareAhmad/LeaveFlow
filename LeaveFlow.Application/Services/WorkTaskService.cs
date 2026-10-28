using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Services
{
    public class WorkTaskService(IWorkTaskRepository repository,
            IEmployeeRepository employeeRepository,
            IUserRepository userRepository,
            IOrganisationAccessService accessService,
            ILeaveRequestRepository leaveRepository,
            INotificationService notifications,
            IAuditService audit,
            IUnitOfWork unitOfWork) : IWorkTaskService
    {
        private static WorkTaskDto MapToDto(WorkTask t)
        {
            return new WorkTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                DepartmentId = t.DepartmentId,
                Department = t.Department.Name,
                AssignedEmployeeId = t.AssignedEmployeeId,
                AssignedEmployee = $"{t.AssignedEmployee.FirstName} {t.AssignedEmployee.LastName}",
                AssignedEmployeeRole = t.AssignedEmployee.User?.Role?.Name ?? string.Empty,
                AssignedByUserId = t.AssignedByUserId,
                AssignedBy = t.AssignedByUser.Email,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt,
                ReviewedAt = t.ReviewedAt,
                ReviewComment = t.ReviewComment,
                ProgressUpdates = t.ProgressUpdates
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new TaskProgressDto
                    {
                        Id = p.Id,
                        EmployeeName = $"{p.Employee.FirstName} {p.Employee.LastName}",
                        Comment = p.Comment,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList()
            };
        }

        public async Task<BaseResponse<WorkTaskDto>> CreateAsync(
            Guid assignerUserId,
            CreateWorkTaskRequest request)
        {
            if (request.DueDate.Date < DateTime.UtcNow.Date)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Due date cannot be in the past." };
            }

            var actor = await accessService.GetContextAsync(assignerUserId);
            var assigner = await userRepository.GetByIdAsync(assignerUserId);
            var target = await employeeRepository.GetByIdAsync(request.AssignedEmployeeId);

            if (!actor.Status || actor.Data == null || assigner == null || target == null)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Assigner or employee not found." };
            }

            // Admin can assign tasks to Managers; Managers can assign tasks to Employees in their department
            bool allowed = false;
            if (actor.Data.IsSystemAdmin)
            {
                if (target.User.Role.Name == RoleNames.Manager)
                {
                    allowed = true;
                }
            }
            else if (actor.Data.RoleName == RoleNames.Manager
                && target.User.Role.Name == RoleNames.Employee
                && target.DepartmentId == actor.Data.DepartmentId)
            {
                allowed = true;
            }

            if (!allowed)
            {
                return new BaseResponse<WorkTaskDto>
                {
                    Message = "You cannot assign a task to this employee."
                };
            }

            var task = new WorkTask
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Priority = request.Priority,
                DepartmentId = target.DepartmentId,
                AssignedEmployeeId = target.Id,
                AssignedByUserId = assignerUserId,
                DueDate = request.DueDate.Date,
                Status = WorkTaskStatus.Assigned,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(task);
            await unitOfWork.SaveChangesAsync();

            // Let the assignee know; warn if due date falls during an approved leave
            var leaves = await leaveRepository.GetByEmployeeIdAsync(target.Id);
            var hasLeaveNearDueDate = leaves.Any(l =>
                l.Status == LeaveStatus.Approved
                && l.StartDate <= task.DueDate
                && l.EndDate >= task.DueDate);

            var message = hasLeaveNearDueDate
                ? $"{task.Title} is due during approved leave."
                : task.Title;

            await notifications.CreateAsync(
                target.UserId,
                "New task assigned",
                message,
                NotificationType.Task,
                $"/WorkTask/Details/{task.Id}");

            await audit.RecordAsync(
                assignerUserId,
                "Created task",
                nameof(WorkTask),
                task.Id,
                task.Title);

            var savedTask = await repository.GetByIdAsync(task.Id);
            return new BaseResponse<WorkTaskDto>
            {
                Status = true,
                Message = "Task assigned successfully.",
                Data = MapToDto(savedTask!)
            };
        }

        public async Task<BaseResponse<List<WorkTaskDto>>> GetMyTasksAsync(Guid employeeId)
        {
            var tasks = await repository.GetAssignedToEmployeeAsync(employeeId);
            var dtos = tasks.Select(MapToDto).ToList();

            return new BaseResponse<List<WorkTaskDto>>
            {
                Status = true,
                Data = dtos
            };
        }

        public async Task<BaseResponse<List<WorkTaskDto>>> GetReviewQueueAsync(Guid reviewerUserId)
        {
            var actor = await accessService.GetContextAsync(reviewerUserId);
            if (!actor.Status || actor.Data == null)
            {
                return new BaseResponse<List<WorkTaskDto>> { Message = actor.Message };
            }

            List<WorkTask> tasks;
            if (actor.Data.IsSystemAdmin)
            {
                tasks = await repository.GetManagerTasksAsync();
            }
            else if (actor.Data.RoleName == RoleNames.Manager)
            {
                tasks = await repository.GetDepartmentTasksAsync(actor.Data.DepartmentId);
            }
            else
            {
                tasks = new List<WorkTask>();
            }

            var dtos = tasks.Select(MapToDto).ToList();
            return new BaseResponse<List<WorkTaskDto>>
            {
                Status = true,
                Data = dtos
            };
        }

        public async Task<BaseResponse<WorkTaskDto>> GetByIdAsync(Guid id, Guid userId)
        {
            var task = await repository.GetByIdAsync(id);
            var actor = await accessService.GetContextAsync(userId);

            if (task == null || !actor.Status || actor.Data == null)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Task not found." };
            }

            if (task.AssignedEmployeeId != actor.Data.EmployeeId
                && !await accessService.CanManageEmployeeAsync(userId, task.AssignedEmployeeId))
            {
                return new BaseResponse<WorkTaskDto> { Message = "You cannot view this task." };
            }

            return new BaseResponse<WorkTaskDto>
            {
                Status = true,
                Data = MapToDto(task)
            };
        }

        public async Task<BaseResponse<WorkTaskDto>> StartAsync(Guid id, Guid employeeId)
        {
            var task = await repository.GetByIdAsync(id);
            if (task == null || task.AssignedEmployeeId != employeeId)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Task not found." };
            }

            if (task.Status != WorkTaskStatus.Assigned && task.Status != WorkTaskStatus.Reopened)
            {
                return new BaseResponse<WorkTaskDto> { Message = "This task cannot be started." };
            }

            task.Status = WorkTaskStatus.InProgress;
            task.StartedAt ??= DateTime.UtcNow;
            repository.Update(task);
            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<WorkTaskDto>
            {
                Status = true,
                Message = "Task started.",
                Data = MapToDto(task)
            };
        }

        public async Task<BaseResponse<WorkTaskDto>> AddProgressAsync(
            Guid id,
            Guid employeeId,
            AddTaskProgressRequest request)
        {
            var task = await repository.GetByIdAsync(id);
            if (task == null || task.AssignedEmployeeId != employeeId)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Task not found." };
            }

            if (task.Status == WorkTaskStatus.Assigned)
            {
                return new BaseResponse<WorkTaskDto>
                {
                    Message = "You must start the task before adding progress updates."
                };
            }

            if (task.Status != WorkTaskStatus.InProgress && task.Status != WorkTaskStatus.Reopened)
            {
                return new BaseResponse<WorkTaskDto>
                {
                    Message = "Progress updates can only be added while the task is in progress."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                return new BaseResponse<WorkTaskDto> { Message = "Progress comment cannot be empty." };
            }

            var update = new TaskProgressUpdate
            {
                WorkTaskId = id,
                EmployeeId = employeeId,
                Comment = request.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddProgressAsync(update);
            await unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id, task.AssignedEmployee.UserId);
        }

        public async Task<BaseResponse<WorkTaskDto>> CompleteAsync(Guid id, Guid employeeId)
        {
            var task = await repository.GetByIdAsync(id);
            if (task == null || task.AssignedEmployeeId != employeeId)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Task not found." };
            }

            if (task.Status != WorkTaskStatus.InProgress && task.Status != WorkTaskStatus.Reopened)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Task must be in progress before completion." };
            }

            task.Status = WorkTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            repository.Update(task);
            await unitOfWork.SaveChangesAsync();

            await notifications.CreateAsync(
                task.AssignedByUserId,
                "Task completed",
                $"{task.AssignedEmployee.FirstName} {task.AssignedEmployee.LastName} completed task '{task.Title}'.",
                NotificationType.Task,
                $"/WorkTask/Details/{id}");

            return new BaseResponse<WorkTaskDto>
            {
                Status = true,
                Message = "Task submitted for review.",
                Data = MapToDto(task)
            };
        }

        public async Task<BaseResponse<WorkTaskDto>> ReviewAsync(
            Guid id,
            Guid reviewerUserId,
            ReviewWorkTaskRequest request)
        {
            var task = await repository.GetByIdAsync(id);
            if (task == null)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Task not found." };
            }

            // Assignee can never review their own task
            var reviewer = await userRepository.GetByIdAsync(reviewerUserId);
            if (reviewer == null || reviewer.Employee.Id == task.AssignedEmployeeId)
            {
                return new BaseResponse<WorkTaskDto> { Message = "You cannot review your own task." };
            }

            if (!await accessService.CanManageEmployeeAsync(reviewerUserId, task.AssignedEmployeeId))
            {
                return new BaseResponse<WorkTaskDto>
                {
                    Message = "You are not authorized to review this task."
                };
            }

            if (task.Status != WorkTaskStatus.Completed)
            {
                return new BaseResponse<WorkTaskDto> { Message = "Only completed tasks can be reviewed." };
            }

            if (request.Accept)
            {
                task.Status = WorkTaskStatus.Accepted;
            }
            else
            {
                task.Status = WorkTaskStatus.Reopened;
                task.CompletedAt = null;
            }

            task.ReviewedAt = DateTime.UtcNow;
            task.ReviewComment = request.Comment?.Trim();

            repository.Update(task);
            await unitOfWork.SaveChangesAsync();

            var actionText = request.Accept ? "accepted" : "reopened";
            await notifications.CreateAsync(
                task.AssignedEmployee.UserId,
                $"Task {actionText}",
                $"Your task '{task.Title}' was {actionText} by the reviewer.",
                NotificationType.Task,
                $"/WorkTask/Details/{id}");

            await audit.RecordAsync(
                reviewerUserId,
                request.Accept ? "Accepted task" : "Reopened task",
                nameof(WorkTask),
                id,
                task.Title);

            var message = request.Accept
                ? "Task accepted successfully."
                : "Task reopened for further work.";

            return new BaseResponse<WorkTaskDto>
            {
                Status = true,
                Message = message,
                Data = MapToDto(task)
            };
        }
    }
}
