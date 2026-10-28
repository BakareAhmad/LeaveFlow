using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Entities;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Services
{
    public class DepartmentService(
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork) : IDepartmentService
    {
        public async Task<BaseResponse<DepartmentDto>> CreateAsync(CreateDepartmentRequest request)
        {
            request.Name = request.Name.Trim();

            if (await departmentRepository.DepartmentNameExistsAsync(request.Name))
            {
                return new BaseResponse<DepartmentDto>
                {
                    Message = "Department already exists."
                };
            }

            var department = request.Adapt<Department>();

            await departmentRepository.AddAsync(department);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<DepartmentDto>
            {
                Status = true,
                Message = "Department created successfully.",
                Data = department.Adapt<DepartmentDto>()
            };
        }

        public async Task<BaseResponse<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request)
        {
            request.Name = request.Name.Trim();

            var department = await departmentRepository.GetByIdAsync(id);

            if (department is null)
            {
                return new BaseResponse<DepartmentDto>
                {
                    Message = "Department not found."
                };
            }

            if (!department.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)
                && await departmentRepository.DepartmentNameExistsAsync(request.Name))
            {
                return new BaseResponse<DepartmentDto>
                {
                    Message = "Department already exists."
                };
            }

            department.Name = request.Name;

            departmentRepository.Update(department);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<DepartmentDto>
            {
                Status = true,
                Message = "Department updated successfully.",
                Data = department.Adapt<DepartmentDto>()
            };
        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid id)
        {
            var department = await departmentRepository.GetByIdAsync(id);

            if (department is null)
            {
                return new BaseResponse<bool>
                {
                    Message = "Department not found."
                };
            }

            department.IsDeleted = true;
            department.DeletedAt = DateTime.UtcNow;

            departmentRepository.Update(department);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Status = true,
                Message = "Department deleted successfully.",
                Data = true
            };
        }

        public async Task<BaseResponse<DepartmentDto>> GetByIdAsync(Guid id)
        {
            var department = await departmentRepository.GetByIdAsync(id);

            if (department is null)
            {
                return new BaseResponse<DepartmentDto>
                {
                    Message = "Department not found."
                };
            }

            return new BaseResponse<DepartmentDto>
            {
                Status = true,
                Message = "Department retrieved successfully.",
                Data = department.Adapt<DepartmentDto>()
            };
        }

        public async Task<BaseResponse<List<DepartmentDto>>> GetAllAsync()
        {
            var departments = await departmentRepository.GetAllAsync();

            return new BaseResponse<List<DepartmentDto>>
            {
                Status = true,
                Message = "Departments retrieved successfully.",
                Data = departments.Adapt<List<DepartmentDto>>()
            };
        }
    }
}
