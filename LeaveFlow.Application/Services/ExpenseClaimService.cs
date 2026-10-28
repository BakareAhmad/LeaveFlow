using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Services
{
    public class ExpenseClaimService(IExpenseClaimRepository repository,
            IEmployeeRepository employeeRepository,
            IWorkTaskRepository workTaskRepository,
            IOrganisationAccessService accessService,
            INotificationService notifications,
            IAuditService audit,
            IUnitOfWork unitOfWork) : IExpenseClaimService
    {
        private static ExpenseClaimDto MapToDto(ExpenseClaim c)
        {
            return new ExpenseClaimDto
            {
                Id = c.Id,
                EmployeeId = c.EmployeeId,
                EmployeeName = $"{c.Employee.FirstName} {c.Employee.LastName}",
                Department = c.Department.Name,
                EmployeeRole = c.Employee.User.Role.Name,
                Title = c.Title,
                Description = c.Description,
                TotalAmount = c.TotalAmount,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                SubmittedAt = c.SubmittedAt,
                AdminComment = c.AdminComment,
                WorkTaskId = c.WorkTaskId,
                Items = c.Items
                    .Where(i => !i.IsDeleted)
                    .Select(i => new ExpenseItemDto
                    {
                        Id = i.Id,
                        Description = i.Description,
                        Amount = i.Amount,
                        ExpenseDate = i.ExpenseDate,
                        Category = i.Category
                    })
                    .ToList(),
                Attachments = c.Attachments
                    .Where(a => !a.IsDeleted)
                    .Select(a => new ExpenseAttachmentDto
                    {
                        Id = a.Id,
                        OriginalFileName = a.OriginalFileName,
                        StoredFileName = a.StoredFileName,
                        ContentType = a.ContentType,
                        FileSize = a.FileSize
                    })
                    .ToList()
            };
        }

        public async Task<BaseResponse<ExpenseClaimDto>> CreateDraftAsync(
            Guid employeeId,
            CreateExpenseClaimRequest request)
        {
            var employee = await employeeRepository.GetByIdAsync(employeeId);
            if (employee == null || employee.UserId == SystemIds.AdminUserId)
            {
                return new BaseResponse<ExpenseClaimDto> { Message = "Employee not found." };
            }

            if (employee.User.Role.Name != RoleNames.Manager)
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "Only managers are permitted to submit expense claims."
                };
            }

            if (request.WorkTaskId.HasValue)
            {
                var task = await workTaskRepository.GetByIdAsync(request.WorkTaskId.Value);
                if (task == null || task.AssignedEmployeeId != employeeId)
                {
                    return new BaseResponse<ExpenseClaimDto>
                    {
                        Message = "The selected task is not assigned to you."
                    };
                }
            }

            var claim = new ExpenseClaim
            {
                EmployeeId = employee.Id,
                DepartmentId = employee.DepartmentId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                WorkTaskId = request.WorkTaskId,
                Status = ExpenseClaimStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(claim);
            await unitOfWork.SaveChangesAsync();
            await audit.RecordAsync(
                employee.UserId,
                "Created expense claim draft",
                nameof(ExpenseClaim),
                claim.Id,
                claim.Title);

            var savedClaim = await repository.GetByIdAsync(claim.Id);
            return new BaseResponse<ExpenseClaimDto>
            {
                Status = true,
                Message = "Expense claim draft created successfully.",
                Data = MapToDto(savedClaim!)
            };
        }

        public async Task<BaseResponse<List<ExpenseClaimDto>>> GetMyClaimsAsync(Guid employeeId)
        {
            var employee = await employeeRepository.GetByIdAsync(employeeId);
            if (employee == null || employee.User.Role.Name != RoleNames.Manager)
            {
                return new BaseResponse<List<ExpenseClaimDto>>
                {
                    Status = true,
                    Data = new List<ExpenseClaimDto>()
                };
            }

            var claims = await repository.GetByEmployeeAsync(employeeId);
            var dtos = claims.Select(MapToDto).ToList();

            return new BaseResponse<List<ExpenseClaimDto>>
            {
                Status = true,
                Data = dtos
            };
        }

        public async Task<BaseResponse<List<ExpenseClaimDto>>> GetReviewQueueAsync(Guid reviewerUserId)
        {
            var actor = await accessService.GetContextAsync(reviewerUserId);
            if (!actor.Status || actor.Data == null)
            {
                return new BaseResponse<List<ExpenseClaimDto>> { Message = actor.Message };
            }

            if (!actor.Data.IsSystemAdmin)
            {
                return new BaseResponse<List<ExpenseClaimDto>>
                {
                    Status = true,
                    Data = new List<ExpenseClaimDto>()
                };
            }

            var claims = await repository.GetForAdminReviewAsync();
            var dtos = claims.Select(MapToDto).ToList();

            return new BaseResponse<List<ExpenseClaimDto>>
            {
                Status = true,
                Data = dtos
            };
        }

        public async Task<BaseResponse<List<ExpenseClaimDto>>> GetClaimHistoryAsync(Guid userId)
        {
            var actor = await accessService.GetContextAsync(userId);
            if (!actor.Status || actor.Data == null)
            {
                return new BaseResponse<List<ExpenseClaimDto>> { Message = actor.Message };
            }

            if (!actor.Data.IsSystemAdmin)
            {
                return new BaseResponse<List<ExpenseClaimDto>>
                {
                    Status = true,
                    Data = new List<ExpenseClaimDto>()
                };
            }

            var claims = await repository.GetAdminClaimHistoryAsync();
            var dtos = claims.Select(MapToDto).ToList();

            return new BaseResponse<List<ExpenseClaimDto>>
            {
                Status = true,
                Data = dtos
            };
        }

        public async Task<BaseResponse<ExpenseClaimDto>> GetByIdAsync(Guid id, Guid userId)
        {
            var claim = await repository.GetByIdAsync(id);
            var actor = await accessService.GetContextAsync(userId);

            if (claim == null || !actor.Status || actor.Data == null)
            {
                return new BaseResponse<ExpenseClaimDto> { Message = "Expense claim not found." };
            }

            var isOwner = claim.EmployeeId == actor.Data.EmployeeId;
            var isAdmin = actor.Data.IsSystemAdmin;

            if (!isOwner && !isAdmin)
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "You do not have permission to view this expense claim."
                };
            }

            return new BaseResponse<ExpenseClaimDto>
            {
                Status = true,
                Data = MapToDto(claim)
            };
        }

        public async Task<BaseResponse<ExpenseClaimDto>> AddItemAsync(
            Guid id,
            Guid employeeId,
            AddExpenseItemRequest request)
        {
            var claim = await repository.GetByIdAsync(id);
            if (claim == null
                || claim.EmployeeId != employeeId
                || claim.Status != ExpenseClaimStatus.Draft)
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "Only the creator can edit a Draft expense claim."
                };
            }

            if (request.Amount <= 0)
            {
                return new BaseResponse<ExpenseClaimDto> { Message = "Expense amount must be greater than zero." };
            }

            if (request.ExpenseDate.Date > DateTime.UtcNow.Date)
            {
                return new BaseResponse<ExpenseClaimDto> { Message = "Expense date cannot be in the future." };
            }

            var item = new ExpenseItem
            {
                ExpenseClaimId = id,
                Description = request.Description.Trim(),
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate.Date,
                Category = request.Category.Trim()
            };

            await repository.AddItemAsync(item);
            claim.TotalAmount += item.Amount;
            claim.Items.Add(item);
            repository.Update(claim);
            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<ExpenseClaimDto>
            {
                Status = true,
                Message = "Expense item added.",
                Data = MapToDto(claim)
            };
        }

        public async Task<BaseResponse<bool>> DeleteItemAsync(
            Guid id,
            Guid itemId,
            Guid employeeId)
        {
            var claim = await repository.GetByIdAsync(id);
            var item = await repository.GetItemByIdAsync(itemId);

            if (claim == null
                || item == null
                || item.ExpenseClaimId != id
                || claim.EmployeeId != employeeId
                || claim.Status != ExpenseClaimStatus.Draft)
            {
                return new BaseResponse<bool> { Message = "Expense item cannot be removed." };
            }

            claim.TotalAmount -= item.Amount;
            repository.DeleteItem(item);
            repository.Update(claim);
            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Status = true,
                Data = true,
                Message = "Expense item removed."
            };
        }

        public async Task<BaseResponse<ExpenseClaimDto>> AddAttachmentAsync(
            Guid id,
            Guid employeeId,
            AddExpenseAttachmentRequest request)
        {
            var claim = await repository.GetByIdAsync(id);
            if (claim == null
                || claim.EmployeeId != employeeId
                || claim.Status != ExpenseClaimStatus.Draft)
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "Receipt cannot be attached to this claim."
                };
            }

            var attachment = new ExpenseAttachment
            {
                ExpenseClaimId = id,
                OriginalFileName = request.OriginalFileName,
                StoredFileName = request.StoredFileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                UploadedAt = DateTime.UtcNow
            };

            await repository.AddAttachmentAsync(attachment);
            claim.Attachments.Add(attachment);
            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<ExpenseClaimDto>
            {
                Status = true,
                Message = "Receipt attached successfully.",
                Data = MapToDto(claim)
            };
        }

        public async Task<BaseResponse<ExpenseClaimDto>> SubmitAsync(Guid id, Guid employeeId)
        {
            var claim = await repository.GetByIdAsync(id);
            if (claim == null
                || claim.EmployeeId != employeeId
                || claim.Status != ExpenseClaimStatus.Draft)
            {
                return new BaseResponse<ExpenseClaimDto> { Message = "Expense claim cannot be submitted." };
            }

            if (claim.Items.Count == 0 || claim.TotalAmount <= 0)
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "Add at least one expense item before submitting."
                };
            }

            claim.Status = ExpenseClaimStatus.Submitted;
            claim.SubmittedAt = DateTime.UtcNow;
            repository.Update(claim);
            await unitOfWork.SaveChangesAsync();

            await notifications.CreateAsync(
                SystemIds.AdminUserId,
                "Expense claim submitted",
                $"{claim.Employee.FirstName} {claim.Employee.LastName} submitted a claim of {claim.TotalAmount:C} for '{claim.Title}'.",
                NotificationType.Expense,
                $"/ExpenseClaim/Details/{id}");

            await audit.RecordAsync(
                claim.Employee.UserId,
                "Submitted expense claim",
                nameof(ExpenseClaim),
                id,
                $"{claim.TotalAmount:0.00}");

            return new BaseResponse<ExpenseClaimDto>
            {
                Status = true,
                Message = "Expense claim submitted to System Administrator for review.",
                Data = MapToDto(claim)
            };
        }

        public async Task<BaseResponse<ExpenseClaimDto>> ReviewAsync(
            Guid id,
            Guid reviewerUserId,
            ReviewExpenseClaimRequest request)
        {
            var claim = await repository.GetByIdAsync(id);
            var actor = await accessService.GetContextAsync(reviewerUserId);

            if (claim == null || !actor.Status || actor.Data == null)
            {
                return new BaseResponse<ExpenseClaimDto> { Message = "Expense claim not found." };
            }

            if (!actor.Data.IsSystemAdmin)
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "Only the system administrator can review expense claims."
                };
            }

            if (claim.Status != ExpenseClaimStatus.Submitted)
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "This expense claim is not awaiting review."
                };
            }

            if (!request.Approve && string.IsNullOrWhiteSpace(request.Comment))
            {
                return new BaseResponse<ExpenseClaimDto>
                {
                    Message = "A reason is required when rejecting an expense claim."
                };
            }

            if (request.Approve)
            {
                claim.Status = ExpenseClaimStatus.AdminApproved;
            }
            else
            {
                claim.Status = ExpenseClaimStatus.AdminRejected;
            }

            claim.AdminReviewedBy = reviewerUserId;
            claim.AdminReviewedAt = DateTime.UtcNow;
            claim.AdminComment = request.Comment?.Trim();

            repository.Update(claim);
            await unitOfWork.SaveChangesAsync();

            var statusWord = request.Approve ? "approved" : "rejected";

            await notifications.CreateAsync(
                claim.Employee.UserId,
                $"Expense claim {statusWord}",
                $"Your claim '{claim.Title}' ({claim.TotalAmount:C}) was {statusWord} by the system administrator.",
                NotificationType.Expense,
                $"/ExpenseClaim/Details/{id}");

            await audit.RecordAsync(
                reviewerUserId,
                request.Approve ? "Approved expense claim" : "Rejected expense claim",
                nameof(ExpenseClaim),
                id,
                claim.Status.ToString());

            return new BaseResponse<ExpenseClaimDto>
            {
                Status = true,
                Message = $"Expense claim {statusWord} successfully.",
                Data = MapToDto(claim)
            };
        }

        public async Task<BaseResponse<ExpenseAttachmentDto>> GetAttachmentAsync(
            Guid attachmentId,
            Guid userId)
        {
            var attachment = await repository.GetAttachmentAsync(attachmentId);
            if (attachment == null)
            {
                return new BaseResponse<ExpenseAttachmentDto>
                {
                    Message = "Receipt attachment not found."
                };
            }

            var access = await GetByIdAsync(attachment.ExpenseClaimId, userId);
            if (!access.Status)
            {
                return new BaseResponse<ExpenseAttachmentDto> { Message = access.Message };
            }

            var dto = new ExpenseAttachmentDto
            {
                Id = attachment.Id,
                OriginalFileName = attachment.OriginalFileName,
                StoredFileName = attachment.StoredFileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize
            };

            return new BaseResponse<ExpenseAttachmentDto>
            {
                Status = true,
                Data = dto
            };
        }
    }
}
