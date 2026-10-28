using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IPerformanceReviewService
    {
        Task<BaseResponse<PerformanceReviewCycleDto>> CreateCycleAsync(Guid adminUserId, CreatePerformanceCycleRequest request);
        Task<BaseResponse<PerformanceReviewCycleDto>> OpenCycleAsync(Guid cycleId, Guid adminUserId);
        Task<BaseResponse<PerformanceReviewCycleDto>> CloseCycleAsync(Guid cycleId, Guid adminUserId);
        Task<BaseResponse<List<PerformanceReviewCycleDto>>> GetCyclesAsync();
        Task<BaseResponse<List<PerformanceReviewDto>>> GetMyReviewsAsync(Guid employeeId);
        Task<BaseResponse<List<PerformanceReviewDto>>> GetReviewQueueAsync(Guid reviewerUserId);
        Task<BaseResponse<PerformanceReviewDto>> GetByIdAsync(Guid id, Guid userId);
        Task<BaseResponse<DepartmentReviewReportDto>> GetDepartmentReportAsync(Guid cycleId, Guid departmentId, Guid userId);
        Task<BaseResponse<List<DepartmentReviewReportDto>>> GetCycleDepartmentReportsAsync(Guid cycleId, Guid adminUserId);
        Task<BaseResponse<PerformanceReviewDto>> SaveManagerReviewAsync(Guid id, Guid reviewerUserId, SaveManagerReviewRequest request);
        Task<BaseResponse<DepartmentReviewReportDto>> SubmitDepartmentReportAsync(Guid cycleId, Guid departmentId, Guid managerUserId, SubmitDepartmentReportRequest request);
    }
}
