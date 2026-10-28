namespace LeaveFlow.Host.Models
{
    public record DashboardViewModel
    {
        public bool IsAuthenticated { get; set; }
        public bool IsSystemAdmin { get; set; }
        public bool CanReview { get; set; }
        public bool HasEmployeeProfile { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public int LeaveBalance { get; set; }
        public int MyPendingLeaveRequests { get; set; }
        public int MyApprovedLeaveRequests { get; set; }
        public int MyRejectedLeaveRequests { get; set; }
        public int MyOpenTasks { get; set; }
        public int MyActiveExpenseClaims { get; set; }
        public int MyOutstandingPerformanceReviews { get; set; }

        public int LeaveRequestsToReview { get; set; }
        public int TasksToReview { get; set; }
        public int ExpenseClaimsToReview { get; set; }
        public int PerformanceReviewsToReview { get; set; }

        public int UnreadNotifications { get; set; }

        public int TotalEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalLeaveTypes { get; set; }

        public int TotalItemsToReview
            => LeaveRequestsToReview
             + TasksToReview
             + ExpenseClaimsToReview
             + PerformanceReviewsToReview;
    }
}
