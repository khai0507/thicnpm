using DocTask.Core.DTOs.ApiResponses;
using DocTask.Core.Dtos.SubTasks;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Paginations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocTask.Core.Exceptions;

namespace DocTask.Api.Controllers
{
    [ApiController]
    [Route("api/v1/subtask")]
    public class SubTaskController : ControllerBase
    {
        private readonly ISubTaskService _subTaskService;
        private readonly IUserRepository _userRepository;

        public SubTaskController(ISubTaskService subTaskService, IUserRepository userRepository, IFrequencyRepository frequencyRepository, IFrequencyDetailRepository frequencyDetailRepository)
        {
            _subTaskService = subTaskService;
            _userRepository = userRepository;
        }

        // POST: api/v1/subtask/{parentTaskId} - Tạo subtask mới
        [HttpPost("{parentTaskId}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> CreateSubTask(int parentTaskId, [FromBody] CreateSubTaskRequest request)
        {
            // Get current user ID for assignerId
            var userId = GetUserIdFromHttpContext();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Không thể xác thực người dùng"
                });
            }

            var subTaskDto = await _subTaskService.CreateAsync(parentTaskId, request, userId);

            return Ok(new ApiResponse<SubTaskDto>
            {
                Success = true,
                Data = subTaskDto,
                Message = "Tạo subtask thành công"
            });
        }

        // GET: api/v1/subtask?key=search_keyword&parentTaskId=32&page=1&size=10
        [HttpGet("by-parent-task/{parentTaskId:int}")]
        public async Task<IActionResult> GetSubTasks(
            [FromRoute] int parentTaskId,
            [FromQuery] string? key,
            [FromQuery] PageOptionsRequest pageOptions
            )
        {

            // Get by parent task ID with pagination
            var subtasks = await _subTaskService.GetAllByParentIdAsync(parentTaskId, pageOptions, key);
            return Ok(new ApiResponse<PaginatedList<SubTaskDto>>
            {
                Success = true,
                Data = subtasks,
                Message = "Lấy danh sách subtasks thành công"
            });
        }


        // GET: api/v1/subtask/assignable-users - Get subordinates and peers for task assignment
        [HttpGet("assignable-users")]
        public async Task<IActionResult> GetAssignableUsers()
        {
            var callerId = GetUserIdFromHttpContext();
            if (callerId == null)
            {
                throw new UnauthorizedException("Không thể xác thực người dùng");
            }

            var (subordinates, peers) = await _userRepository.GetSubordinatesAndPeersAsync(callerId.Value);
            var result = new { Subordinates = subordinates, Peers = peers };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = result,
                Message = "Lấy danh sách cấp dưới và đồng nghiệp để giao việc thành công"
            });
        }

        [HttpGet("assigned")]
        [Authorize]
        public async Task<IActionResult> GetMySubTasks([FromQuery] PageOptionsRequest pageOptions, [FromQuery] string? key)
        {
            var userId = GetUserIdFromHttpContext();
            if (userId == null)
            {
                throw new UnauthorizedException("Không thể xác thực người dùng");
            }

            var subtasks = await _subTaskService.GetByAssignedUserIdPaginatedAsync(userId.Value, key, pageOptions);

            return Ok(new ApiResponse<PaginatedList<SubTaskDto>>
            {
                Success = true,
                Data = subtasks,
                Message = "Lấy danh sách subtask của bạn thành công"
            });
        }


        // PUT: api/v1/subtask/{parentTaskId}?subTaskId=33
        [HttpPut]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateSubTask([FromQuery] int subTaskId, [FromBody] UpdateSubTaskRequest request)
        {
            var updatedSubTask = await _subTaskService.UpdateSubtask(subTaskId, request);
            if (updatedSubTask == null)
            {
                throw new NotFoundException("Subtask không tồn tại");
            }

            return Ok(new ApiResponse<SubTaskDto>
            {
                Success = true,
                Data = updatedSubTask,
                Message = "Cập nhật subtask thành công"
            });
        }

        // DELETE: api/v1/subtask?subTaskId=33
        [HttpDelete]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteSubTask([FromQuery] int subTaskId)
        {
            //phân quyền
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                throw new UnauthorizedException("Không thể xác thực người dùng");
            }
            int userId = int.Parse(userIdClaim);

            //Check điều kiện
            if (subTaskId <= 0)
            {
                throw new BadRequestException("SubTaskId không hợp lệ");
            }
            var success = await _subTaskService.DeleteAsync(subTaskId, userId);
            if (!success)
            {
                throw new NotFoundException("Subtask không tồn tại");
            }
            //Xóa thành công
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Xóa subtask thành công"
            });
        }
        private int? GetUserIdFromHttpContext()
        {
            var idClaim = HttpContext.User.FindFirst("id");
            if (idClaim == null) return null;
            if (int.TryParse(idClaim.Value, out var id)) return id;
            return null;
        }
    }
}