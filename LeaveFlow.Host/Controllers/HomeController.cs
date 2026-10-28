using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Host.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace LeaveFlow.Host.Controllers
{
    public class HomeController(
        IEmployeeService employeeService,
        IDepartmentService departmentService,
        ILeaveTypeService leaveTypeService,
        ILeaveRequestService leaveRequestService,
        IWorkTaskService workTaskService,
        IExpenseClaimService expenseClaimService,
        IPerformanceReviewService performanceReviewService,
        INotificationService notificationService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                Name = User.Identity?.Name ?? string.Empty,
                Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
            };

            if (!model.IsAuthenticated)
            {
                return View(model);
            }

            // An authenticated dashboard must use the trusted user ID from the login cookie.
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Challenge();
            }

            model.IsSystemAdmin = userId == SystemIds.AdminUserId;
            model.CanReview = model.IsSystemAdmin || User.IsInRole(RoleNames.Manager);
            model.UnreadNotifications = (await notificationService.GetUnreadCountAsync(userId)).Data;

            // Employees and Managers have a personal workspace. The system Admin does not.
            if (!model.IsSystemAdmin
                && Guid.TryParse(User.FindFirstValue("EmployeeId"), out var employeeId))
            {
                model.HasEmployeeProfile = true;

                var employeeResponse = await employeeService.GetByIdAsync(employeeId);
                model.LeaveBalance = employeeResponse.Data?.LeaveBalance ?? 0;

                var myLeaveRequests = (await leaveRequestService.GetByEmployeeIdAsync(employeeId)).Data ?? [];
                model.MyPendingLeaveRequests = myLeaveRequests.Count(r => r.Status == LeaveStatus.Pending);
                model.MyApprovedLeaveRequests = myLeaveRequests.Count(r => r.Status == LeaveStatus.Approved);
                model.MyRejectedLeaveRequests = myLeaveRequests.Count(r => r.Status == LeaveStatus.Rejected);

                var myTasks = (await workTaskService.GetMyTasksAsync(employeeId)).Data ?? [];
                model.MyOpenTasks = myTasks.Count(t => t.Status != WorkTaskStatus.Accepted);

                // Only managers create expense claims
                if (User.IsInRole(RoleNames.Manager))
                {
                    var myExpenseClaims = (await expenseClaimService.GetMyClaimsAsync(employeeId)).Data ?? [];
                    model.MyActiveExpenseClaims = myExpenseClaims.Count(e =>
                        e.Status == ExpenseClaimStatus.Draft
                        || e.Status == ExpenseClaimStatus.Submitted);
                }

                // Only regular staff employees are appraised
                if (User.IsInRole(RoleNames.Employee))
                {
                    var myPerformanceReviews = (await performanceReviewService.GetMyReviewsAsync(employeeId)).Data ?? [];
                    model.MyOutstandingPerformanceReviews = myPerformanceReviews.Count;
                }
            }

            // Review counts stay separate so each card matches the queue it opens.
            if (model.CanReview)
            {
                var leaveQueue = (await leaveRequestService.GetReviewableAsync(userId)).Data ?? [];
                model.LeaveRequestsToReview = leaveQueue.Count(r => r.Status == LeaveStatus.Pending);

                var taskQueue = (await workTaskService.GetReviewQueueAsync(userId)).Data ?? [];
                model.TasksToReview = taskQueue.Count(t => t.Status == WorkTaskStatus.Completed);

                // Only the System Admin reviews expense claims submitted by Managers
                if (model.IsSystemAdmin)
                {
                    var expenseQueue = (await expenseClaimService.GetReviewQueueAsync(userId)).Data ?? [];
                    model.ExpenseClaimsToReview = expenseQueue.Count(c => c.Status == ExpenseClaimStatus.Submitted);
                }

                // Only Department Managers review departmental staff performance
                if (User.IsInRole(RoleNames.Manager))
                {
                    var performanceQueue = (await performanceReviewService.GetReviewQueueAsync(userId)).Data ?? [];
                    model.PerformanceReviewsToReview = performanceQueue.Count(r => r.Status != PerformanceReviewStatus.Submitted);
                }
            }

            if (model.IsSystemAdmin)
            {
                var employeesResponse = await employeeService.GetAllAsync();
                var departmentsResponse = await departmentService.GetAllAsync();
                var leaveTypesResponse = await leaveTypeService.GetAllAsync();

                model.TotalEmployees = employeesResponse.Data?.Count ?? 0;
                model.TotalDepartments = departmentsResponse.Data?.Count ?? 0;
                model.TotalLeaveTypes = leaveTypesResponse.Data?.Count ?? 0;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
