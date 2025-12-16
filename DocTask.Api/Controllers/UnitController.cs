using DocTask.Core.DTOs.ApiResponses;
using DocTask.Core.Dtos.Units;
using DocTask.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace DocTask.Api.Controllers;

[ApiController]
[Route("/api/v1/units")]
[Authorize]
public class UnitController : ControllerBase
{
    private readonly IUnitService _unitService;
    private readonly ILogger<UnitController> _logger;

    public UnitController(IUnitService unitService, ILogger<UnitController> logger)
    {
        _unitService = unitService;
        _logger = logger;
    }

    // GET: api/v1/units/assignable
    [HttpGet("assignable")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetAssignableUnits()
    {
        try
        {
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Không thể xác định người dùng."
                });
            }

            // Lấy đơn vị của user hiện tại
            var userUnit = await _unitService.GetUserUnitAsync(userId);
            if (userUnit == null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Bạn không thuộc đơn vị nào. Vui lòng liên hệ quản trị viên để được phân quyền."
                });
            }

            // Lấy danh sách đơn vị có thể giao việc
            var assignableUnits = await _unitService.GetAssignableUnitsAsync(userUnit.UnitId);
            
            return Ok(new ApiResponse<List<UnitDto>>
            {
                Success = true,
                Data = assignableUnits,
                Message = "Lấy danh sách đơn vị có thể giao việc thành công."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignable units");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Error = "Lỗi hệ thống khi lấy danh sách đơn vị có thể giao việc."
            });
        }
    }

    // POST: api/v1/units/create-task
    [HttpPost("create-task")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> CreateTaskForUnit([FromBody] CreateTaskForUnitRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Không thể xác định người dùng."
                });
            }

            var result = await _unitService.CreateTaskForUnitAsync(request, userId);
            if (result == null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Không thể tạo việc cho đơn vị."
                });
            }

            return Ok(new ApiResponse<UnitTaskDto>
            {
                Success = true,
                Data = result,
                Message = "Tạo việc cho đơn vị thành công."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<string>
            {
                Success = false,
                Error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task for unit");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Error = "Lỗi hệ thống khi tạo việc cho đơn vị."
            });
        }
    }
}