using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Services
{
    public class EmployeeService(
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository,
    IRoleRepository roleRepository,
    ILeaveAccrualSettingRepository accrualSettingRepository,
    IUnitOfWork unitOfWork) : IEmployeeService
    {
        private static EmployeeDto MapToDto(Employee employee)
        {
            return MapToDto(employee, employee.Department, employee.User.Role);
        }

        private static EmployeeDto MapToDto(
            Employee employee,
            Department department,
            Role role)
        {
            return new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.User.Email,
                DepartmentId = employee.DepartmentId,
                Department = department.Name,
                RoleId = employee.User.RoleId,
                Role = role.Name,
                LeaveBalance = employee.LeaveBalance
            };
        }

        public async Task<BaseResponse<EmployeeDto>> CreateAsync(RegisterEmployeeRequest request)
        {
            request.FirstName = request.FirstName.Trim();
            request.LastName = request.LastName.Trim();
            request.PhoneNumber = request.PhoneNumber.Trim();
            request.Email = request.Email.Trim().ToLower();

            if (await userRepository.EmailExistsAsync(request.Email))
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Email already exists."
                };
            }

            var department = await departmentRepository.GetByIdAsync(request.DepartmentId);

            if (department is null)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Department not found."
                };
            }

            var role = await roleRepository.GetByIdAsync(request.RoleId);

            if (role is null)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Role not found."
                };
            }

            if (role.Name == RoleNames.Admin)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "The Admin role is reserved for the system administrator."
                };
            }

            if (role.Name == RoleNames.Manager
                && await employeeRepository.ManagerExistsInDepartmentAsync(
                    request.DepartmentId))
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "This department already has a manager."
                };
            }

            var accrualSetting = await accrualSettingRepository.GetLatestAsync(
                DateTime.UtcNow.Year);

            if (accrualSetting is null)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "The current annual leave accrual amount has not been configured."
                };
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = request.RoleId
            };

            var employee = new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                DepartmentId = request.DepartmentId,
                LeaveBalance = accrualSetting.AccrualAmount,
                UserId = user.Id,
                User = user
            };

            await userRepository.AddAsync(user);
            await employeeRepository.AddAsync(employee);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<EmployeeDto>
            {
                Status = true,
                Message = "Employee created successfully.",
                Data = MapToDto(employee, department, role)
            };
        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid id)
        {
            var employee = await employeeRepository.GetByIdAsync(id);

            if (employee is null)
            {
                return new BaseResponse<bool>
                {
                    Message = "Employee not found."
                };
            }

            if (employee.Id == SystemIds.AdminEmployeeId)
            {
                return new BaseResponse<bool>
                {
                    Message = "The system administrator account cannot be deleted."
                };
            }

            var deletedAt = DateTime.UtcNow;

            employee.IsDeleted = true;
            employee.DeletedAt = deletedAt;
            employee.User.IsDeleted = true;
            employee.User.DeletedAt = deletedAt;

            employeeRepository.Update(employee);
            userRepository.Update(employee.User);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Status = true,
                Message = "Employee deleted successfully.",
                Data = true
            };
        }

        public async Task<BaseResponse<List<EmployeeDto>>> GetAllAsync()
        {
            var employees = await employeeRepository.GetAllAsync();

            return new BaseResponse<List<EmployeeDto>>
            {
                Status = true,
                Message = "Employees retrieved.",
                Data = employees.Select(employee => MapToDto(employee)).ToList()
            };
        }

        public async Task<BaseResponse<EmployeeDto>> GetByIdAsync(Guid id)
        {
            var employee = await employeeRepository.GetByIdAsync(id);

            if (employee is null)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Employee not found."
                };
            }

            return new BaseResponse<EmployeeDto>
            {
                Status = true,
                Message = "Employee retrieved successfully.",
                Data = MapToDto(employee)
            };
        }

        public async Task<BaseResponse<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeRequest request)
        {
            request.FirstName = request.FirstName.Trim();
            request.LastName = request.LastName.Trim();
            request.PhoneNumber = request.PhoneNumber.Trim();
            request.Email = request.Email.Trim().ToLower();

            var employee = await employeeRepository.GetByIdAsync(id);

            if (employee is null)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Employee not found."
                };
            }

            if (employee.Id == SystemIds.AdminEmployeeId)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "The system administrator account cannot be edited."
                };
            }

            var department = await departmentRepository.GetByIdAsync(request.DepartmentId);

            if (department is null)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Department not found."
                };
            }

            var role = await roleRepository.GetByIdAsync(request.RoleId);

            if (role is null)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Role not found."
                };
            }

            if (role.Name == RoleNames.Admin)
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "The Admin role is reserved for the system administrator."
                };
            }

            if (role.Name == RoleNames.Manager
                && await employeeRepository.ManagerExistsInDepartmentAsync(
                    request.DepartmentId,
                    employee.Id))
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "This department already has a manager."
                };
            }

            if (!employee.User.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)
                && await userRepository.EmailExistsAsync(request.Email))
            {
                return new BaseResponse<EmployeeDto>
                {
                    Message = "Email already exists."
                };
            }

            employee.FirstName = request.FirstName;
            employee.LastName = request.LastName;
            employee.PhoneNumber = request.PhoneNumber;
            employee.DepartmentId = request.DepartmentId;

            employee.User.Email = request.Email;
            employee.User.RoleId = request.RoleId;

            employeeRepository.Update(employee);
            userRepository.Update(employee.User);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<EmployeeDto>
            {
                Status = true,
                Message = "Employee updated successfully.",
                Data = MapToDto(employee, department, role)
            };
        }
    }
}
