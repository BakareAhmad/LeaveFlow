using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IAuditService
    {
        Task RecordAsync(Guid? userId, string action, string entityType, Guid? entityId, string? details = null);
        Task<BaseResponse<List<AuditLogDto>>> GetRecentAsync();
    }

    public interface IOperationsReportService
    {
        Task<BaseResponse<OperationsReportDto>> GetAsync(Guid userId, OperationsReportFilterRequest filter);
        Task<BaseResponse<string>> ExportCsvAsync(Guid userId, OperationsReportFilterRequest filter);
        Task<BaseResponse<byte[]>> ExportPdfAsync(Guid userId, OperationsReportFilterRequest filter);
    }
}
