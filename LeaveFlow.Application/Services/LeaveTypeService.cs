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
    public class LeaveTypeService(
    ILeaveTypeRepository leaveTypeRepository,
    IUnitOfWork unitOfWork) : ILeaveTypeService
    {
        public async Task<BaseResponse<LeaveTypeDto>> CreateAsync(CreateLeaveTypeRequest request)
        {
            request.Name = request.Name.Trim();
            request.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            if (await leaveTypeRepository.LeaveTypeNameExistsAsync(request.Name))
            {
                return new BaseResponse<LeaveTypeDto>
                {
                    Message = "Leave type already exists."
                };
            }

            var leaveType = request.Adapt<LeaveType>();

            await leaveTypeRepository.AddAsync(leaveType);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<LeaveTypeDto>
            {
                Status = true,
                Message = "Leave type created successfully.",
                Data = leaveType.Adapt<LeaveTypeDto>()
            };
        }

        public async Task<BaseResponse<LeaveTypeDto>> UpdateAsync(Guid id, UpdateLeaveTypeRequest request)
        {
            request.Name = request.Name.Trim();
            request.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            var leaveType = await leaveTypeRepository.GetByIdAsync(id);

            if (leaveType is null)
            {
                return new BaseResponse<LeaveTypeDto>
                {
                    Message = "Leave type not found."
                };
            }

            if (!leaveType.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)
                && await leaveTypeRepository.LeaveTypeNameExistsAsync(request.Name))
            {
                return new BaseResponse<LeaveTypeDto>
                {
                    Message = "Leave type already exists."
                };
            }

            leaveType.Name = request.Name;
            leaveType.MaximumDays = request.MaximumDays;
            leaveType.Description = request.Description;

            leaveTypeRepository.Update(leaveType);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<LeaveTypeDto>
            {
                Status = true,
                Message = "Leave type updated successfully.",
                Data = leaveType.Adapt<LeaveTypeDto>()
            };
        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid id)
        {
            var leaveType = await leaveTypeRepository.GetByIdAsync(id);

            if (leaveType is null)
            {
                return new BaseResponse<bool>
                {
                    Message = "Leave type not found."
                };
            }

            leaveType.IsDeleted = true;
            leaveType.DeletedAt = DateTime.UtcNow;

            leaveTypeRepository.Update(leaveType);

            await unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Status = true,
                Message = "Leave type deleted successfully.",
                Data = true
            };
        }

        public async Task<BaseResponse<LeaveTypeDto>> GetByIdAsync(Guid id)
        {
            var leaveType = await leaveTypeRepository.GetByIdAsync(id);

            if (leaveType is null)
            {
                return new BaseResponse<LeaveTypeDto>
                {
                    Message = "Leave type not found."
                };
            }

            return new BaseResponse<LeaveTypeDto>
            {
                Status = true,
                Message = "Leave type retrieved successfully.",
                Data = leaveType.Adapt<LeaveTypeDto>()
            };
        }

        public async Task<BaseResponse<List<LeaveTypeDto>>> GetAllAsync()
        {
            var leaveTypes = await leaveTypeRepository.GetAllAsync();

            return new BaseResponse<List<LeaveTypeDto>>
            {
                Status = true,
                Message = "Leave types retrieved successfully.",
                Data = leaveTypes.Adapt<List<LeaveTypeDto>>()
            };
        }
    }
}
