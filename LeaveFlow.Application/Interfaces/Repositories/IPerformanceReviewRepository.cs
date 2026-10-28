using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IPerformanceReviewRepository
    {
        Task AddCycleAsync(PerformanceReviewCycle cycle);
        Task<PerformanceReviewCycle?> GetCycleAsync(Guid id);
        Task<List<PerformanceReviewCycle>> GetCyclesAsync();
        Task AddReviewAsync(PerformanceReview review);
        Task<PerformanceReview?> GetReviewAsync(Guid id);
        Task<PerformanceReview?> GetReviewByCycleAndEmployeeAsync(Guid cycleId, Guid employeeId);
        Task<List<PerformanceReview>> GetEmployeeReviewsAsync(Guid employeeId);
        Task<List<PerformanceReview>> GetDepartmentReviewsAsync(Guid cycleId, Guid departmentId);
        Task<List<PerformanceReview>> GetReviewsByCycleAsync(Guid cycleId);
        Task<bool> ReviewExistsAsync(Guid cycleId, Guid employeeId);
        Task<bool> HasOverlappingCycleAsync(DateTime startDate, DateTime endDate, Guid? excludedCycleId = null);
        void UpdateCycle(PerformanceReviewCycle cycle);
        void UpdateReview(PerformanceReview review);

        Task<DepartmentReviewReport?> GetDepartmentReportAsync(Guid cycleId, Guid departmentId);
        Task<List<DepartmentReviewReport>> GetDepartmentReportsByCycleAsync(Guid cycleId);
        Task AddDepartmentReportAsync(DepartmentReviewReport report);
        void UpdateDepartmentReport(DepartmentReviewReport report);
    }
}
