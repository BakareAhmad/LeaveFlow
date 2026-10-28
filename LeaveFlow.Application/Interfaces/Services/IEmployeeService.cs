using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<BaseResponse<EmployeeDto>> CreateAsync(RegisterEmployeeRequest request);

        Task<BaseResponse<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeRequest request);

        Task<BaseResponse<bool>> DeleteAsync(Guid id);

        Task<BaseResponse<EmployeeDto>> GetByIdAsync(Guid id);

        Task<BaseResponse<List<EmployeeDto>>> GetAllAsync();
    }
}
