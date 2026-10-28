using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IDepartmentService
    {
        Task<BaseResponse<DepartmentDto>> CreateAsync(CreateDepartmentRequest request);

        Task<BaseResponse<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request);

        Task<BaseResponse<bool>> DeleteAsync(Guid id);

        Task<BaseResponse<DepartmentDto>> GetByIdAsync(Guid id);

        Task<BaseResponse<List<DepartmentDto>>> GetAllAsync();
    }
}
