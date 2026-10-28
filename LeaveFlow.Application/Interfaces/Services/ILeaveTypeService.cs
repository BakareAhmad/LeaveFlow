using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface ILeaveTypeService
    {
        Task<BaseResponse<LeaveTypeDto>> CreateAsync(CreateLeaveTypeRequest request);

        Task<BaseResponse<LeaveTypeDto>> UpdateAsync(Guid id, UpdateLeaveTypeRequest request);

        Task<BaseResponse<bool>> DeleteAsync(Guid id);

        Task<BaseResponse<LeaveTypeDto>> GetByIdAsync(Guid id);

        Task<BaseResponse<List<LeaveTypeDto>>> GetAllAsync();
    }
}
