using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<BaseResponse<EmployeeDto>> LoginAsync(LoginRequest request);

        Task<BaseResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    }
}
