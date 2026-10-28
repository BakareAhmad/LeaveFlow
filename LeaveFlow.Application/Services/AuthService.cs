using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Services
{
    public class AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork) : IAuthService
    {
        private static EmployeeDto MapToDto(User user)
        {
            return new EmployeeDto
            {
                Id = user.Employee.Id,
                UserId = user.Id,
                FirstName = user.Employee.FirstName,
                LastName = user.Employee.LastName,
                Email = user.Email,
                PhoneNumber = user.Employee.PhoneNumber,
                DepartmentId = user.Employee.DepartmentId,
                Department = user.Employee.Department.Name,
                RoleId = user.RoleId,
                Role = user.Role.Name,
                LeaveBalance = user.Employee.LeaveBalance
            };
        }

        public async Task<BaseResponse<EmployeeDto>> LoginAsync(LoginRequest request)
        {
            request.Email = request.Email.Trim().ToLower();

            var user = await userRepository.GetByEmailAsync(request.Email);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Invalid email or password."
                };
            }

            return new BaseResponse<EmployeeDto>
            {
                Status = true,
                Message = "Login successful.",
                Data = MapToDto(user)
            };
        }

        public async Task<BaseResponse<bool>> ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequest request)
        {
            var user = await userRepository.GetByIdAsync(userId);

            if (user is null)
            {
                return new BaseResponse<bool>
                {
                    Message = "User not found."
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return new BaseResponse<bool>
                {
                    Message = "Current password is incorrect."
                };
            }

            if (!request.NewPassword.Equals(request.ConfirmPassword, StringComparison.Ordinal))
            {
                return new BaseResponse<bool>
                {
                    Message = "New password and confirmation password do not match."
                };
            }

            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            {
                return new BaseResponse<bool>
                {
                    Message = "New password must be different from the current password."
                };
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            userRepository.Update(user);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Status = true,
                Message = "Password changed successfully.",
                Data = true
            };
        }
    }
}
