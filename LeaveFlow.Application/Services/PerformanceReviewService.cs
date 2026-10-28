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
    public class PerformanceReviewService(IPerformanceReviewRepository repository,
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IWorkTaskRepository workTaskRepository,
            ILeaveRequestRepository leaveRepository,
            IOrganisationAccessService accessService,
            INotificationService notifications,
            IAuditService audit,
            IUnitOfWork unitOfWork) : IPerformanceReviewService
    {
        private async Task<EmployeeTaskPerformanceDto> CalculateTaskPerformanceAsync(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate)
        {
            var tasks = await workTaskRepository.GetAssignedToEmployeeAsync(employeeId);

            var completedTasks = tasks
                .Where(t =>
                {
                    if (t.Status != WorkTaskStatus.Completed && t.Status != WorkTaskStatus.Accepted)
                        return false;

                    // Count a task if it was completed within the review period.
                    // CompletedAt is preferred; fall back to other fields when missing.
                    if (t.CompletedAt.HasValue)
                    {
                        return t.CompletedAt.Value.Date >= startDate.Date
                            && t.CompletedAt.Value.Date <= endDate.Date;
                    }

                    if (t.CreatedAt.Date >= startDate.Date && t.CreatedAt.Date <= endDate.Date)
                        return true;

                    return t.DueDate.Date >= startDate.Date && t.DueDate.Date <= endDate.Date;
                })
                .ToList();

            var total = completedTasks.Count;
            var onTime = completedTasks.Count(t =>
            {
                if (t.CompletedAt.HasValue)
                    return t.CompletedAt.Value.Date <= t.DueDate.Date;
                return true; // no completion date on record, treat as on time
            });
            var late = total - onTime;

            double? percentage = null;
            if (total > 0)
            {
                percentage = System.Math.Round((double)onTime / total * 100.0, 1);
            }

            return new EmployeeTaskPerformanceDto
            {
                TotalCompletedTasks = total,
                CompletedOnTime = onTime,
                CompletedLate = late,
                OnTimeCompletionPercentage = percentage
            };
        }

        private async Task<EmployeeLeaveActivityDto> CalculateLeaveActivityAsync(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate)
        {
            var leaves = await leaveRepository.GetByEmployeeIdAsync(employeeId);

            var nonAnnualLeaves = leaves
                .Where(l =>
                {
                    if (l.LeaveType.Name.Contains("Annual", System.StringComparison.OrdinalIgnoreCase))
                        return false;

                    var overlapsPeriod = l.StartDate.Date <= endDate.Date
                        && l.EndDate.Date >= startDate.Date;
                    var appliedInPeriod = l.AppliedDate.Date >= startDate.Date
                        && l.AppliedDate.Date <= endDate.Date;

                    return overlapsPeriod || appliedInPeriod;
                })
                .ToList();

            return new EmployeeLeaveActivityDto
            {
                TotalNonAnnualLeaveRequests = nonAnnualLeaves.Count,
                Approved = nonAnnualLeaves.Count(l => l.Status == LeaveStatus.Approved),
                Rejected = nonAnnualLeaves.Count(l => l.Status == LeaveStatus.Rejected),
                Pending = nonAnnualLeaves.Count(l => l.Status == LeaveStatus.Pending)
            };
        }

        private async Task<PerformanceReviewDto> MapReviewAsync(PerformanceReview r)
        {
            var taskPerf = await CalculateTaskPerformanceAsync(
                r.EmployeeId,
                r.Cycle.StartDate,
                r.Cycle.EndDate);
            var leaveAct = await CalculateLeaveActivityAsync(
                r.EmployeeId,
                r.Cycle.StartDate,
                r.Cycle.EndDate);

            var departmentReport = await repository.GetDepartmentReportAsync(
                r.CycleId,
                r.Employee.DepartmentId);

            string? reviewerName = null;
            if (r.Reviewer != null)
            {
                if (r.Reviewer.Employee != null)
                {
                    reviewerName = $"{r.Reviewer.Employee.FirstName} {r.Reviewer.Employee.LastName}";
                }
                else
                {
                    reviewerName = r.Reviewer.Email;
                }
            }

            var dto = new PerformanceReviewDto
            {
                Id = r.Id,
                CycleId = r.CycleId,
                CycleName = r.Cycle.Name,
                StartDate = r.Cycle.StartDate,
                EndDate = r.Cycle.EndDate,
                EmployeeId = r.EmployeeId,
                EmployeeName = $"{r.Employee.FirstName} {r.Employee.LastName}",
                DepartmentId = r.Employee.DepartmentId,
                Department = r.Employee.Department.Name,
                EmployeeRole = r.Employee.User?.Role?.Name ?? string.Empty,
                ReviewerUserId = r.ReviewerUserId,
                ReviewerName = reviewerName,
                Status = r.Status,
                TaskPerformance = taskPerf,
                LeaveActivity = leaveAct,
                ManagerComments = r.ManagerComments,
                ManagerReviewedAt = r.ManagerReviewedAt,
                IsDepartmentReportSubmitted = departmentReport?.IsSubmitted == true
            };

            return dto;
        }

        private static PerformanceReviewCycleDto MapCycle(
            PerformanceReviewCycle c,
            int submittedReports = 0,
            int totalDepts = 0)
        {
            var completed = c.Reviews.Count(r =>
                r.Status == PerformanceReviewStatus.Completed
                || r.Status == PerformanceReviewStatus.Submitted);

            return new PerformanceReviewCycleDto
            {
                Id = c.Id,
                Name = c.Name,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsOpen = c.IsOpen,
                TotalReviews = c.Reviews.Count,
                CompletedReviews = completed,
                TotalDepartments = totalDepts,
                SubmittedDepartmentReports = submittedReports
            };
        }

        public async Task<BaseResponse<PerformanceReviewCycleDto>> CreateCycleAsync(
            Guid adminUserId,
            CreatePerformanceCycleRequest request)
        {
            if (adminUserId != SystemIds.AdminUserId)
            {
                return new BaseResponse<PerformanceReviewCycleDto>
                {
                    Message = "Only the system administrator can create review cycles."
                };
            }

            request.Name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new BaseResponse<PerformanceReviewCycleDto>
                {
                    Message = "Review cycle name is required."
                };
            }

            if (request.EndDate.Date < request.StartDate.Date)
            {
                return new BaseResponse<PerformanceReviewCycleDto>
                {
                    Message = "End date cannot be earlier than start date."
                };
            }

            var overlaps = await repository.HasOverlappingCycleAsync(
                request.StartDate,
                request.EndDate);
            if (overlaps)
            {
                return new BaseResponse<PerformanceReviewCycleDto>
                {
                    Message = "The selected review period overlaps with an existing review cycle. Review cycles cannot have overlapping date ranges."
                };
            }

            var cycle = new PerformanceReviewCycle
            {
                Name = request.Name,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                IsOpen = false,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddCycleAsync(cycle);
            await unitOfWork.SaveChangesAsync();
            await audit.RecordAsync(
                adminUserId,
                "Created performance cycle",
                nameof(PerformanceReviewCycle),
                cycle.Id,
                cycle.Name);

            return new BaseResponse<PerformanceReviewCycleDto>
            {
                Status = true,
                Data = MapCycle(cycle),
                Message = "Review cycle created successfully."
            };
        }

        public async Task<BaseResponse<PerformanceReviewCycleDto>> OpenCycleAsync(
            Guid cycleId,
            Guid adminUserId)
        {
            if (adminUserId != SystemIds.AdminUserId)
            {
                return new BaseResponse<PerformanceReviewCycleDto>
                {
                    Message = "Only the system administrator can open review cycles."
                };
            }

            var cycle = await repository.GetCycleAsync(cycleId);
            if (cycle == null)
            {
                return new BaseResponse<PerformanceReviewCycleDto> { Message = "Review cycle not found." };
            }

            if (cycle.IsOpen)
            {
                return new BaseResponse<PerformanceReviewCycleDto> { Message = "Cycle is already open." };
            }

            // Department Managers review employees in their department. Admin does not review managers.
            var allEmployees = await employeeRepository.GetAllAsync();
            var reviewableEmployees = allEmployees
                .Where(e => e.User.Role.Name == RoleNames.Employee && e.Id != SystemIds.AdminEmployeeId)
                .ToList();

            foreach (var employee in reviewableEmployees)
            {
                var alreadyExists = await repository.ReviewExistsAsync(cycle.Id, employee.Id);
                if (!alreadyExists)
                {
                    var review = new PerformanceReview
                    {
                        CycleId = cycle.Id,
                        EmployeeId = employee.Id,
                        Status = PerformanceReviewStatus.NotStarted
                    };
                    await repository.AddReviewAsync(review);
                }
            }

            cycle.IsOpen = true;
            repository.UpdateCycle(cycle);
            await unitOfWork.SaveChangesAsync();

            // Notify managers about the new cycle
            var managers = allEmployees
                .Where(e => e.User.Role.Name == RoleNames.Manager && e.Id != SystemIds.AdminEmployeeId)
                .ToList();

            var managerUserIds = managers.Select(m => m.UserId);
            await notifications.CreateManyAsync(
                managerUserIds,
                "Performance Review Cycle Opened",
                $"Review cycle '{cycle.Name}' is now active. Please review your department employees and submit the department report.",
                NotificationType.Performance,
                $"/PerformanceReview/DepartmentReport?cycleId={cycle.Id}");

            await audit.RecordAsync(
                adminUserId,
                "Opened performance cycle",
                nameof(PerformanceReviewCycle),
                cycle.Id,
                cycle.Name);

            var refreshed = await repository.GetCycleAsync(cycleId);
            return new BaseResponse<PerformanceReviewCycleDto>
            {
                Status = true,
                Data = MapCycle(refreshed!),
                Message = "Review cycle opened successfully."
            };
        }

        public async Task<BaseResponse<PerformanceReviewCycleDto>> CloseCycleAsync(
            Guid cycleId,
            Guid adminUserId)
        {
            if (adminUserId != SystemIds.AdminUserId)
            {
                return new BaseResponse<PerformanceReviewCycleDto>
                {
                    Message = "Only the system administrator can close review cycles."
                };
            }

            var cycle = await repository.GetCycleAsync(cycleId);
            if (cycle == null)
            {
                return new BaseResponse<PerformanceReviewCycleDto> { Message = "Review cycle not found." };
            }

            cycle.IsOpen = false;
            repository.UpdateCycle(cycle);
            await unitOfWork.SaveChangesAsync();
            await audit.RecordAsync(
                adminUserId,
                "Closed performance cycle",
                nameof(PerformanceReviewCycle),
                cycle.Id,
                cycle.Name);

            return new BaseResponse<PerformanceReviewCycleDto>
            {
                Status = true,
                Data = MapCycle(cycle),
                Message = "Review cycle closed successfully."
            };
        }

        public async Task<BaseResponse<List<PerformanceReviewCycleDto>>> GetCyclesAsync()
        {
            var cycles = await repository.GetCyclesAsync();
            var departments = await departmentRepository.GetAllAsync();
            var nonAdminDepts = departments
                .Where(d => d.Id != SystemIds.AdministrationDepartmentId)
                .ToList();

            var result = new List<PerformanceReviewCycleDto>();
            foreach (var cycle in cycles)
            {
                var reports = await repository.GetDepartmentReportsByCycleAsync(cycle.Id);
                var submittedReports = reports.Count(r => r.IsSubmitted);
                result.Add(MapCycle(cycle, submittedReports, nonAdminDepts.Count));
            }

            return new BaseResponse<List<PerformanceReviewCycleDto>>
            {
                Status = true,
                Data = result
            };
        }

        public async Task<BaseResponse<List<PerformanceReviewDto>>> GetMyReviewsAsync(Guid employeeId)
        {
            var reviews = await repository.GetEmployeeReviewsAsync(employeeId);
            var result = new List<PerformanceReviewDto>();
            foreach (var r in reviews)
            {
                var mapped = await MapReviewAsync(r);
                result.Add(mapped);
            }

            return new BaseResponse<List<PerformanceReviewDto>>
            {
                Status = true,
                Data = result
            };
        }

        public async Task<BaseResponse<List<PerformanceReviewDto>>> GetReviewQueueAsync(Guid reviewerUserId)
        {
            var actor = await accessService.GetContextAsync(reviewerUserId);
            if (!actor.Status || actor.Data == null)
            {
                return new BaseResponse<List<PerformanceReviewDto>> { Message = actor.Message };
            }

            if (actor.Data.RoleName != RoleNames.Manager)
            {
                return new BaseResponse<List<PerformanceReviewDto>>
                {
                    Status = true,
                    Data = new List<PerformanceReviewDto>()
                };
            }

            var openCycles = (await repository.GetCyclesAsync())
                .Where(c => c.IsOpen)
                .ToList();

            var result = new List<PerformanceReviewDto>();
            foreach (var cycle in openCycles)
            {
                var deptReviews = await repository.GetDepartmentReviewsAsync(
                    cycle.Id,
                    actor.Data.DepartmentId);
                foreach (var r in deptReviews)
                {
                    var mapped = await MapReviewAsync(r);
                    result.Add(mapped);
                }
            }

            return new BaseResponse<List<PerformanceReviewDto>>
            {
                Status = true,
                Data = result
            };
        }

        public async Task<BaseResponse<PerformanceReviewDto>> GetByIdAsync(Guid id, Guid userId)
        {
            var review = await repository.GetReviewAsync(id);
            var actor = await accessService.GetContextAsync(userId);

            if (review == null || !actor.Status || actor.Data == null)
            {
                return new BaseResponse<PerformanceReviewDto> { Message = "Performance review not found." };
            }

            var isEmployee = review.EmployeeId == actor.Data.EmployeeId;
            var isDeptManager = actor.Data.RoleName == RoleNames.Manager
                && review.Employee.DepartmentId == actor.Data.DepartmentId;
            var isAdmin = actor.Data.IsSystemAdmin;

            if (!isEmployee && !isDeptManager && !isAdmin)
            {
                return new BaseResponse<PerformanceReviewDto>
                {
                    Message = "You do not have permission to view this performance review."
                };
            }

            var mapped = await MapReviewAsync(review);
            return new BaseResponse<PerformanceReviewDto>
            {
                Status = true,
                Data = mapped
            };
        }

        public async Task<BaseResponse<PerformanceReviewDto>> SaveManagerReviewAsync(
            Guid id,
            Guid reviewerUserId,
            SaveManagerReviewRequest request)
        {
            var review = await repository.GetReviewAsync(id);
            var actor = await accessService.GetContextAsync(reviewerUserId);

            if (review == null || !actor.Status || actor.Data == null)
            {
                return new BaseResponse<PerformanceReviewDto> { Message = "Performance review not found." };
            }

            if (actor.Data.RoleName != RoleNames.Manager
                || review.Employee.DepartmentId != actor.Data.DepartmentId
                || review.EmployeeId == actor.Data.EmployeeId)
            {
                return new BaseResponse<PerformanceReviewDto>
                {
                    Message = "You are only permitted to review employees within your department."
                };
            }

            if (!review.Cycle.IsOpen)
            {
                return new BaseResponse<PerformanceReviewDto> { Message = "This review cycle is closed." };
            }

            var departmentReport = await repository.GetDepartmentReportAsync(
                review.CycleId,
                review.Employee.DepartmentId);
            if (departmentReport?.IsSubmitted == true)
            {
                return new BaseResponse<PerformanceReviewDto>
                {
                    Message = "The departmental review report has already been submitted to Admin and cannot be edited."
                };
            }

            review.ManagerComments = request.ManagerComments.Trim();
            review.ReviewerUserId = reviewerUserId;
            review.ManagerReviewedAt = DateTime.UtcNow;
            review.Status = request.MarkAsCompleted
                ? PerformanceReviewStatus.Completed
                : PerformanceReviewStatus.Draft;

            repository.UpdateReview(review);
            await unitOfWork.SaveChangesAsync();

            await audit.RecordAsync(
                reviewerUserId,
                "Updated employee review comment",
                nameof(PerformanceReview),
                review.Id,
                $"{review.Employee.FirstName} {review.Employee.LastName}");

            var mapped = await MapReviewAsync(review);
            return new BaseResponse<PerformanceReviewDto>
            {
                Status = true,
                Message = "Review comment saved successfully.",
                Data = mapped
            };
        }

        public async Task<BaseResponse<DepartmentReviewReportDto>> GetDepartmentReportAsync(
            Guid cycleId,
            Guid departmentId,
            Guid userId)
        {
            var actor = await accessService.GetContextAsync(userId);
            if (!actor.Status || actor.Data == null)
            {
                return new BaseResponse<DepartmentReviewReportDto> { Message = actor.Message };
            }

            if (!actor.Data.IsSystemAdmin
                && (actor.Data.RoleName != RoleNames.Manager || actor.Data.DepartmentId != departmentId))
            {
                return new BaseResponse<DepartmentReviewReportDto>
                {
                    Message = "You do not have access to this department report."
                };
            }

            var cycle = await repository.GetCycleAsync(cycleId);
            var department = await departmentRepository.GetByIdAsync(departmentId);
            if (cycle == null || department == null)
            {
                return new BaseResponse<DepartmentReviewReportDto> { Message = "Cycle or department not found." };
            }

            var managerEmployee = await employeeRepository.GetManagerByDepartmentAsync(departmentId);
            var departmentReviews = await repository.GetDepartmentReviewsAsync(cycleId, departmentId);

            var employeeReviews = new List<PerformanceReviewDto>();
            int totalCompletedTasks = 0;
            int totalOnTimeTasks = 0;
            int totalNonAnnualLeaves = 0;
            int totalApprovedLeaves = 0;
            int totalRejectedLeaves = 0;

            foreach (var r in departmentReviews)
            {
                var mapped = await MapReviewAsync(r);
                employeeReviews.Add(mapped);

                totalCompletedTasks += mapped.TaskPerformance.TotalCompletedTasks;
                totalOnTimeTasks += mapped.TaskPerformance.CompletedOnTime;
                totalNonAnnualLeaves += mapped.LeaveActivity.TotalNonAnnualLeaveRequests;
                totalApprovedLeaves += mapped.LeaveActivity.Approved;
                totalRejectedLeaves += mapped.LeaveActivity.Rejected;
            }

            var reportEntity = await repository.GetDepartmentReportAsync(cycleId, departmentId);

            double? averageOnTime = null;
            if (totalCompletedTasks > 0)
            {
                averageOnTime = System.Math.Round(
                    (double)totalOnTimeTasks / totalCompletedTasks * 100.0,
                    1);
            }

            string managerName = "Unassigned";
            string managerEmail = "N/A";
            if (managerEmployee != null)
            {
                managerName = $"{managerEmployee.FirstName} {managerEmployee.LastName}";
                if (managerEmployee.User != null)
                {
                    managerEmail = managerEmployee.User.Email;
                }
            }

            var completedReviews = employeeReviews.Count(r =>
                r.Status == PerformanceReviewStatus.Completed
                || r.Status == PerformanceReviewStatus.Submitted);

            var dto = new DepartmentReviewReportDto
            {
                CycleId = cycle.Id,
                CycleName = cycle.Name,
                StartDate = cycle.StartDate,
                EndDate = cycle.EndDate,
                DepartmentId = department.Id,
                DepartmentName = department.Name,
                ManagerUserId = managerEmployee?.UserId,
                ManagerName = managerName,
                ManagerEmail = managerEmail,
                SubmittedAt = reportEntity?.SubmittedAt,
                IsSubmitted = reportEntity?.IsSubmitted == true,
                TotalEmployees = employeeReviews.Count,
                ReviewsCompleted = completedReviews,
                AverageOnTimeCompletionPercentage = averageOnTime,
                TotalNonAnnualLeaveRequests = totalNonAnnualLeaves,
                TotalNonAnnualLeaveApproved = totalApprovedLeaves,
                TotalNonAnnualLeaveRejected = totalRejectedLeaves,
                DepartmentComment = reportEntity?.DepartmentComment,
                EmployeeReviews = employeeReviews
            };

            return new BaseResponse<DepartmentReviewReportDto>
            {
                Status = true,
                Data = dto
            };
        }

        public async Task<BaseResponse<List<DepartmentReviewReportDto>>> GetCycleDepartmentReportsAsync(
            Guid cycleId,
            Guid adminUserId)
        {
            if (adminUserId != SystemIds.AdminUserId)
            {
                return new BaseResponse<List<DepartmentReviewReportDto>>
                {
                    Message = "Only the system administrator can view all department reports."
                };
            }

            var cycle = await repository.GetCycleAsync(cycleId);
            if (cycle == null)
            {
                return new BaseResponse<List<DepartmentReviewReportDto>> { Message = "Cycle not found." };
            }

            var departments = (await departmentRepository.GetAllAsync())
                .Where(d => d.Id != SystemIds.AdministrationDepartmentId)
                .ToList();

            var list = new List<DepartmentReviewReportDto>();
            foreach (var dept in departments)
            {
                var report = await GetDepartmentReportAsync(cycleId, dept.Id, adminUserId);
                if (report.Status && report.Data != null)
                {
                    list.Add(report.Data);
                }
            }

            return new BaseResponse<List<DepartmentReviewReportDto>>
            {
                Status = true,
                Data = list
            };
        }

        public async Task<BaseResponse<DepartmentReviewReportDto>> SubmitDepartmentReportAsync(
            Guid cycleId,
            Guid departmentId,
            Guid managerUserId,
            SubmitDepartmentReportRequest request)
        {
            var actor = await accessService.GetContextAsync(managerUserId);
            if (!actor.Status
                || actor.Data == null
                || actor.Data.RoleName != RoleNames.Manager
                || actor.Data.DepartmentId != departmentId)
            {
                return new BaseResponse<DepartmentReviewReportDto>
                {
                    Message = "Only the department manager can submit the department review report."
                };
            }

            var cycle = await repository.GetCycleAsync(cycleId);
            if (cycle == null || !cycle.IsOpen)
            {
                return new BaseResponse<DepartmentReviewReportDto>
                {
                    Message = "This review cycle is not active."
                };
            }

            var department = await departmentRepository.GetByIdAsync(departmentId);
            if (department == null)
            {
                return new BaseResponse<DepartmentReviewReportDto> { Message = "Department not found." };
            }

            var report = await repository.GetDepartmentReportAsync(cycleId, departmentId);
            if (report == null)
            {
                report = new DepartmentReviewReport
                {
                    CycleId = cycleId,
                    DepartmentId = departmentId,
                    ManagerUserId = managerUserId,
                    DepartmentComment = request.DepartmentComment?.Trim(),
                    IsSubmitted = true,
                    SubmittedAt = DateTime.UtcNow
                };
                await repository.AddDepartmentReportAsync(report);
            }
            else
            {
                if (report.IsSubmitted)
                {
                    return new BaseResponse<DepartmentReviewReportDto>
                    {
                        Message = "This department review report has already been submitted to Admin."
                    };
                }

                report.DepartmentComment = request.DepartmentComment?.Trim();
                report.ManagerUserId = managerUserId;
                report.IsSubmitted = true;
                report.SubmittedAt = DateTime.UtcNow;
                repository.UpdateDepartmentReport(report);
            }

            // Mark individual reviews in this department as Submitted
            var reviews = await repository.GetDepartmentReviewsAsync(cycleId, departmentId);
            foreach (var rev in reviews)
            {
                rev.Status = PerformanceReviewStatus.Submitted;
                repository.UpdateReview(rev);
            }

            await unitOfWork.SaveChangesAsync();

            // Notify Admin
            await notifications.CreateAsync(
                SystemIds.AdminUserId,
                "Department Review Report Submitted",
                $"Manager for {department.Name} submitted the appraisal report for '{cycle.Name}'.",
                NotificationType.Performance,
                $"/PerformanceReview/DepartmentReport?cycleId={cycleId}&departmentId={departmentId}");

            // Notify employees
            var employeeUserIds = reviews.Select(r => r.Employee.UserId);
            await notifications.CreateManyAsync(
                employeeUserIds,
                "Performance Review Report Submitted",
                $"Your manager has submitted the departmental review report for '{cycle.Name}' to Administration.",
                NotificationType.Performance,
                "/PerformanceReview/MyReviews");

            await audit.RecordAsync(
                managerUserId,
                "Submitted department review report",
                nameof(DepartmentReviewReport),
                report.Id,
                $"{department.Name} - {cycle.Name}");

            return await GetDepartmentReportAsync(cycleId, departmentId, managerUserId);
        }
    }
}
