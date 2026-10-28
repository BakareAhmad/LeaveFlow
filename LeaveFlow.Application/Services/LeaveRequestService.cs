using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Services
{
    public class LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        ILeaveTypeRepository leaveTypeRepository,
        IUserRepository userRepository,
        INotificationService notifications,
        IAuditService audit,
        IUnitOfWork unitOfWork) : ILeaveRequestService
    {
        private static int CalculateWeekdays(DateTime startDate, DateTime endDate)
        {
            var numberOfDays = 0;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday
                    && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    numberOfDays++;
                }
            }

            return numberOfDays;
        }

        private static LeaveRequestDto MapToDto(LeaveRequest leaveRequest)
        {
            return MapToDto(
                leaveRequest,
                leaveRequest.Employee,
                leaveRequest.LeaveType,
                leaveRequest.Reviewer?.Email);
        }

        private static LeaveRequestDto MapToDto(
            LeaveRequest leaveRequest,
            Employee employee,
            LeaveType leaveType,
            string? reviewerEmail)
        {
            return new LeaveRequestDto
            {
                Id = leaveRequest.Id,
                EmployeeId = leaveRequest.EmployeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Department = employee.Department.Name,
                LeaveTypeId = leaveRequest.LeaveTypeId,
                LeaveType = leaveType.Name,
                StartDate = leaveRequest.StartDate,
                EndDate = leaveRequest.EndDate,
                NumberOfDays = leaveRequest.NumberOfDays,
                Reason = leaveRequest.Reason,
                Status = leaveRequest.Status,
                AppliedDate = leaveRequest.AppliedDate,
                ReviewedBy = reviewerEmail,
                ReviewedDate = leaveRequest.ReviewedDate,
                ReviewComment = leaveRequest.ReviewComment
            };
        }

        private static string? ValidateDates(
            DateTime startDate,
            DateTime endDate,
            int numberOfDays)
        {
            if (startDate < DateTime.UtcNow.Date)
            {
                return "Start date cannot be in the past.";
            }

            if (endDate < startDate)
            {
                return "End date cannot be earlier than start date.";
            }

            if (numberOfDays == 0)
            {
                return "The selected date range must include at least one weekday.";
            }

            return null;
        }

        private static bool CanReview(User reviewer, LeaveRequest leaveRequest)
        {
            if (reviewer.Id == SystemIds.AdminUserId
                && reviewer.Role.Name == RoleNames.Admin)
            {
                return leaveRequest.Employee.User.Role.Name == RoleNames.Manager;
            }

            if (reviewer.Role.Name == RoleNames.Manager)
            {
                return leaveRequest.Employee.User.Role.Name == RoleNames.Employee
                    && leaveRequest.Employee.DepartmentId == reviewer.Employee.DepartmentId
                    && leaveRequest.EmployeeId != reviewer.Employee.Id;
            }

            return false;
        }

        public async Task<BaseResponse<LeaveRequestDto>> ApplyAsync(
            Guid employeeId,
            ApplyLeaveRequest request)
        {
            request.StartDate = request.StartDate.Date;
            request.EndDate = request.EndDate.Date;
            request.Reason = request.Reason.Trim();

            var numberOfDays = CalculateWeekdays(request.StartDate, request.EndDate);
            var dateValidationMessage = ValidateDates(
                request.StartDate,
                request.EndDate,
                numberOfDays);

            if (dateValidationMessage is not null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = dateValidationMessage
                };
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Reason is required."
                };
            }

            var employee = await employeeRepository.GetByIdAsync(employeeId);

            if (employee is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Employee not found."
                };
            }

            if (employee.UserId == SystemIds.AdminUserId)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "The system administrator does not submit leave requests."
                };
            }

            var leaveType = await leaveTypeRepository.GetByIdAsync(request.LeaveTypeId);

            if (leaveType is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Leave type not found."
                };
            }

            if (numberOfDays > leaveType.MaximumDays)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = $"This leave type allows a maximum of {leaveType.MaximumDays} days."
                };
            }

            if (numberOfDays > employee.LeaveBalance)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Requested days exceed the available leave balance."
                };
            }

            if (await leaveRequestRepository.HasOverlappingRequestAsync(
                employeeId,
                request.StartDate,
                request.EndDate))
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "The selected dates overlap an existing pending or approved request."
                };
            }

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveTypeId = request.LeaveTypeId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                NumberOfDays = numberOfDays,
                Reason = request.Reason,
                Status = LeaveStatus.Pending,
                AppliedDate = DateTime.UtcNow
            };

            await leaveRequestRepository.AddAsync(leaveRequest);
            await unitOfWork.SaveChangesAsync();

            var reviewerUserId = employee.User.Role.Name == RoleNames.Manager
                ? SystemIds.AdminUserId
                : (await employeeRepository.GetManagerByDepartmentAsync(employee.DepartmentId))?.UserId;
            if (reviewerUserId.HasValue)
                await notifications.CreateAsync(reviewerUserId.Value, "Leave request submitted", $"{employee.FirstName} {employee.LastName}", NotificationType.Leave, $"/LeaveRequest/Details/{leaveRequest.Id}");
            await audit.RecordAsync(employee.UserId, "Submitted leave request", nameof(LeaveRequest), leaveRequest.Id, $"{leaveRequest.StartDate:d} - {leaveRequest.EndDate:d}");

            return new BaseResponse<LeaveRequestDto>
            {
                Status = true,
                Message = "Leave request submitted successfully.",
                Data = MapToDto(leaveRequest, employee, leaveType, null)
            };
        }

        public async Task<BaseResponse<LeaveRequestDto>> UpdateAsync(
            Guid id,
            Guid employeeId,
            UpdateLeaveRequest request)
        {
            var leaveRequest = await leaveRequestRepository.GetByIdAsync(id);

            if (leaveRequest is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Leave request not found."
                };
            }

            if (leaveRequest.EmployeeId != employeeId)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "You cannot update another employee's leave request."
                };
            }

            if (leaveRequest.Status != LeaveStatus.Pending)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Only pending leave requests can be updated."
                };
            }

            request.StartDate = request.StartDate.Date;
            request.EndDate = request.EndDate.Date;
            request.Reason = request.Reason.Trim();

            var numberOfDays = CalculateWeekdays(request.StartDate, request.EndDate);
            var dateValidationMessage = ValidateDates(
                request.StartDate,
                request.EndDate,
                numberOfDays);

            if (dateValidationMessage is not null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = dateValidationMessage
                };
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Reason is required."
                };
            }

            var employee = await employeeRepository.GetByIdAsync(employeeId);

            if (employee is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Employee not found."
                };
            }

            if (employee.UserId == SystemIds.AdminUserId)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "The system administrator does not submit leave requests."
                };
            }

            var leaveType = await leaveTypeRepository.GetByIdAsync(request.LeaveTypeId);

            if (leaveType is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Leave type not found."
                };
            }

            if (numberOfDays > leaveType.MaximumDays)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = $"This leave type allows a maximum of {leaveType.MaximumDays} days."
                };
            }

            if (numberOfDays > employee.LeaveBalance)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Requested days exceed the available leave balance."
                };
            }

            if (await leaveRequestRepository.HasOverlappingRequestAsync(
                employeeId,
                request.StartDate,
                request.EndDate,
                id))
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "The selected dates overlap an existing pending or approved request."
                };
            }

            leaveRequest.LeaveTypeId = request.LeaveTypeId;
            leaveRequest.StartDate = request.StartDate;
            leaveRequest.EndDate = request.EndDate;
            leaveRequest.NumberOfDays = numberOfDays;
            leaveRequest.Reason = request.Reason;

            leaveRequestRepository.Update(leaveRequest);
            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<LeaveRequestDto>
            {
                Status = true,
                Message = "Leave request updated successfully.",
                Data = MapToDto(
                    leaveRequest,
                    employee,
                    leaveType,
                    leaveRequest.Reviewer?.Email)
            };
        }

        public async Task<BaseResponse<bool>> CancelAsync(Guid id, Guid employeeId)
        {
            var leaveRequest = await leaveRequestRepository.GetByIdAsync(id);

            if (leaveRequest is null)
            {
                return new BaseResponse<bool>
                {
                    Message = "Leave request not found."
                };
            }

            if (leaveRequest.EmployeeId != employeeId)
            {
                return new BaseResponse<bool>
                {
                    Message = "You cannot cancel another employee's leave request."
                };
            }

            if (leaveRequest.Status != LeaveStatus.Pending)
            {
                return new BaseResponse<bool>
                {
                    Message = "Only pending leave requests can be cancelled."
                };
            }

            leaveRequest.IsDeleted = true;
            leaveRequest.DeletedAt = DateTime.UtcNow;

            leaveRequestRepository.Update(leaveRequest);
            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Status = true,
                Message = "Leave request cancelled successfully.",
                Data = true
            };
        }

        public async Task<BaseResponse<LeaveRequestDto>> ReviewAsync(
            Guid id,
            Guid reviewerUserId,
            ReviewLeaveRequest request)
        {
            if (request.Status != LeaveStatus.Approved
                && request.Status != LeaveStatus.Rejected)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "A leave request can only be approved or rejected."
                };
            }

            var leaveRequest = await leaveRequestRepository.GetByIdAsync(id);

            if (leaveRequest is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Leave request not found."
                };
            }

            if (leaveRequest.Status != LeaveStatus.Pending)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "This leave request has already been reviewed."
                };
            }

            var reviewer = await userRepository.GetByIdAsync(reviewerUserId);

            if (reviewer is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Reviewer not found."
                };
            }

            if (!CanReview(reviewer, leaveRequest))
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "You are not allowed to review this leave request."
                };
            }

            if (request.Status == LeaveStatus.Approved)
            {
                var employee = await employeeRepository.GetByIdAsync(leaveRequest.EmployeeId);

                if (employee is null)
                {
                    return new BaseResponse<LeaveRequestDto>
                    {
                        Message = "Employee not found."
                    };
                }

                if (leaveRequest.NumberOfDays > employee.LeaveBalance)
                {
                    return new BaseResponse<LeaveRequestDto>
                    {
                        Message = "The employee no longer has enough leave balance for this request."
                    };
                }

                employee.LeaveBalance -= leaveRequest.NumberOfDays;
                employeeRepository.Update(employee);
            }

            request.ReviewComment = string.IsNullOrWhiteSpace(request.ReviewComment)
                ? null
                : request.ReviewComment.Trim();

            leaveRequest.Status = request.Status;
            leaveRequest.ReviewedBy = reviewerUserId;
            leaveRequest.ReviewedDate = DateTime.UtcNow;
            leaveRequest.ReviewComment = request.ReviewComment;

            leaveRequestRepository.Update(leaveRequest);
            await unitOfWork.SaveChangesAsync();

            await notifications.CreateAsync(leaveRequest.Employee.UserId, $"Leave request {request.Status}", leaveRequest.LeaveType.Name, NotificationType.Leave, $"/LeaveRequest/Details/{leaveRequest.Id}");
            await audit.RecordAsync(reviewerUserId, "Reviewed leave request", nameof(LeaveRequest), leaveRequest.Id, request.Status.ToString());

            return new BaseResponse<LeaveRequestDto>
            {
                Status = true,
                Message = request.Status == LeaveStatus.Approved
                    ? "Leave request approved successfully."
                    : "Leave request rejected successfully.",
                Data = MapToDto(
                    leaveRequest,
                    leaveRequest.Employee,
                    leaveRequest.LeaveType,
                    reviewer.Email)
            };
        }

        public async Task<BaseResponse<LeaveRequestDto>> GetByIdAsync(Guid id)
        {
            var leaveRequest = await leaveRequestRepository.GetByIdAsync(id);

            if (leaveRequest is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Leave request not found."
                };
            }

            return new BaseResponse<LeaveRequestDto>
            {
                Status = true,
                Message = "Leave request retrieved successfully.",
                Data = MapToDto(leaveRequest)
            };
        }

        public async Task<BaseResponse<List<LeaveRequestDto>>> GetByEmployeeIdAsync(Guid employeeId)
        {
            var leaveRequests = await leaveRequestRepository.GetByEmployeeIdAsync(employeeId);

            return new BaseResponse<List<LeaveRequestDto>>
            {
                Status = true,
                Message = "Leave requests retrieved successfully.",
                Data = leaveRequests.Select(MapToDto).ToList()
            };
        }

        public async Task<BaseResponse<List<LeaveRequestDto>>> GetReviewableAsync(
            Guid reviewerUserId)
        {
            var reviewer = await userRepository.GetByIdAsync(reviewerUserId);

            if (reviewer is null)
            {
                return new BaseResponse<List<LeaveRequestDto>>
                {
                    Message = "Reviewer not found."
                };
            }

            List<LeaveRequest> leaveRequests;

            if (reviewer.Id == SystemIds.AdminUserId
                && reviewer.Role.Name == RoleNames.Admin)
            {
                leaveRequests = await leaveRequestRepository.GetForReviewAsync(
                    RoleNames.Manager);
            }
            else if (reviewer.Role.Name == RoleNames.Manager)
            {
                leaveRequests = await leaveRequestRepository.GetForReviewAsync(
                    RoleNames.Employee,
                    reviewer.Employee.DepartmentId);
            }
            else
            {
                return new BaseResponse<List<LeaveRequestDto>>
                {
                    Message = "You are not allowed to review leave requests."
                };
            }

            return new BaseResponse<List<LeaveRequestDto>>
            {
                Status = true,
                Message = "Leave requests retrieved successfully.",
                Data = leaveRequests.Select(MapToDto).ToList()
            };
        }

        public async Task<BaseResponse<LeaveRequestDto>> GetReviewableByIdAsync(
            Guid id,
            Guid reviewerUserId)
        {
            var reviewer = await userRepository.GetByIdAsync(reviewerUserId);
            var leaveRequest = await leaveRequestRepository.GetByIdAsync(id);

            if (reviewer is null || leaveRequest is null)
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "Leave request not found."
                };
            }

            if (!CanReview(reviewer, leaveRequest))
            {
                return new BaseResponse<LeaveRequestDto>
                {
                    Message = "You are not allowed to review this leave request."
                };
            }

            return new BaseResponse<LeaveRequestDto>
            {
                Status = true,
                Message = "Leave request retrieved successfully.",
                Data = MapToDto(leaveRequest)
            };
        }
    }
}
