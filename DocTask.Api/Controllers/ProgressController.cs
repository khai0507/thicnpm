using DocTask.Core.DTOs.ApiResponses;
using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocTask.Api.Controllers;

[ApiController]
[Route("/api/v1/progress")]
[Authorize] // Require authentication for all endpoints
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;
    private readonly ITaskPermissionService _taskPermissionService;
    private readonly IProgressCalculationService _progressCalculationService;
    private readonly ILogger<ProgressController> _logger;

    public ProgressController(IProgressService progressService, ITaskPermissionService taskPermissionService, IProgressCalculationService progressCalculationService, ILogger<ProgressController> logger)
    {
        _progressService = progressService;
        _taskPermissionService = taskPermissionService;
        _progressCalculationService = progressCalculationService;
        _logger = logger;
    }

    // ACCEPT: api/v1/progress/{progressId}/accept
    [HttpPost("{progressId}/accept")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> Accept(int progressId)
    {
        // Lấy user ID từ JWT token
                    var userIdClaim = User.FindFirst("id");

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new ApiResponse<string>
            {
                Success = false,
                Error = "Không thể xác định người dùng."
            });
        }

        var progress = await _progressService.GetProgressByIdAsync(progressId);
        if (progress == null)
            return NotFound(new ApiResponse<string> { Success = false, Error = "Không tìm thấy tiến độ." });

        // Chỉ người giao task mới có quyền chấp nhận
        if (!await _taskPermissionService.CanDeleteTaskAsync(userId, progress.TaskId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<string>
            {
                Success = false,
                Error = "Bạn không có quyền chấp nhận báo cáo này."
            });
        }

        // Cập nhật status = submited
        var update = new UpdateProgressRequest
        {
            Proposal = progress.Proposal,
            Result = progress.Result,
            Feedback = progress.Feedback,
            Status = "completed",
            ReportFileName = progress.FileName,
            ReportFileStream = null,
            SubmittedByUserId = progress.UpdatedBy ?? userId // giữ nguyên người nộp ban đầu, fallback hiện tại nếu null
        };

        // Không được ghi đè người nộp (UpdatedBy). Sử dụng đúng người đã nộp ban đầu
        var updated = await _progressService.UpdateProgressEntryAsync(progressId, update, progress.UpdatedBy);
        if (updated == null)
            return NotFound(new ApiResponse<string> { Success = false, Error = "Không tìm thấy tiến độ." });

        // Tính lại và lưu % tiến độ cho task liên quan (bao gồm cha nếu có)
        await _progressCalculationService.CalculateTaskProgressAsync(updated.TaskId);

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "Chấp nhận báo cáo và cập nhật tiến độ thành công."
        });
    }
    // Removed calculate endpoint per requirement

    // CREATE: api/v1/progress?taskId={taskId}
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Admin,User")] // All authenticated users can create progress
    public async Task<IActionResult> Create([FromQuery] int taskId, [FromForm] DocTask.Core.Dtos.Tasks.UpdateProgressFormDto form)
    {
        try
        {
            // Lấy user ID từ JWT token
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Không thể xác định người dùng."
                });
            }

            // Kiểm tra xem task có phải là task con (có thể thêm tiến độ) không
            if (!await _taskPermissionService.CanAddProgressAsync(taskId))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new ApiResponse<string>
                {
                    Success = false,
                    Error = "Chỉ có thể thêm tiến độ cho công việc con. Không thể thêm tiến độ cho công việc cha."
                });
            }

            // Kiểm tra quyền thêm tiến độ
            if (!await _taskPermissionService.CanAddProgressAsync(userId, taskId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<string>
                {
                    Success = false,
                    Error = "Bạn không có quyền thêm tiến độ cho task này."
                });
            }

            // Kiểm tra quyền nộp báo cáo
            if (!await _taskPermissionService.CanSubmitReportAsync(userId, taskId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<string>
                {
                    Success = false,
                    Error = "Bạn không có quyền nộp báo cáo cho task này."
                });
            }

            // Kiểm tra lịch trình nộp báo cáo
            if (!await _taskPermissionService.CanSubmitReportByScheduleAsync(userId, taskId))
            {
                return StatusCode(StatusCodes.Status409Conflict, new ApiResponse<string>
                {
                    Success = false,
                    Error = "Bạn chỉ có thể nộp báo cáo trong thời gian hiệu lực của công việc."
                });
            }

            Stream? fileStream = null;
            string? fileName = null;
            if (form.ReportFile != null && form.ReportFile.Length > 0)
            {
                fileStream = form.ReportFile.OpenReadStream();
                fileName = form.ReportFile.FileName;
            }

            var request = new UpdateProgressRequest
            {
                Proposal = form.Proposal,
                Result = form.Result,
                Feedback = form.Feedback,
                Status = "in_progress",
                ReportFileName = fileName,
                ReportFileStream = fileStream,
                SubmittedByUserId = userId // Sử dụng userId từ token thay vì form
            };

            var result = await _progressService.UpdateProgressAsync(taskId, request, userId);

            // Sau khi tạo báo cáo, tính lại và lưu % tiến độ cho task và task cha nếu có
            await _progressCalculationService.CalculateTaskProgressAsync(taskId);
            return Ok(new ApiResponse<UpdateProgressResponse>
            {
                Success = true,
                Data = result,
                Message = "Tạo tiến độ thành công."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<string>
            {
                Success = false,
                Error = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<string>
            {
                Success = false,
                Error = ex.Message
            });
        }
    }


    // REVIEW SUBTASK PROGRESS: api/v1/progress/review-subtask/{taskId}
    [HttpGet("review/{taskId}")]
    public async Task<IActionResult> ReviewSubTaskProgress(
        int taskId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        [FromQuery] int? assigneeId)
    {
        _logger.LogInformation($"[CONTROLLER-DEBUG] ReviewSubTaskProgress called for task {taskId}");
        
        // Lấy user ID từ JWT token
        var userIdClaim = User.FindFirst("id");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            _logger.LogInformation($"[CONTROLLER-DEBUG] Authentication failed for task {taskId}");
            return Unauthorized(new ApiResponse<string>
            {
                Success = false,
                Error = "Không thể xác định người dùng."
            });
        }
        
        _logger.LogInformation($"[CONTROLLER-DEBUG] User {userId} requesting review for task {taskId}");

        // Kiểm tra quyền xem task
        if (!await _taskPermissionService.CanViewTaskAsync(userId, taskId))
        {
            _logger.LogInformation($"[CONTROLLER-DEBUG] Permission denied for user {userId} on task {taskId}");
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<string>
            {
                Success = false,
                Error = "Bạn không có quyền xem báo cáo của task này.."
            });
        }

        _logger.LogInformation($"[CONTROLLER-DEBUG] Calling ReviewSubTaskProgressAsync for task {taskId}");
        var items = await _progressService.ReviewSubTaskProgressAsync(taskId, from, to, status, assigneeId);
        _logger.LogInformation($"[CONTROLLER-DEBUG] ReviewSubTaskProgressAsync returned {items?.Count ?? 0} items");
        if (items == null || items.Count == 0)
        {
            return Ok(new ApiResponse<List<SubTaskProgressReviewDto>>
            {
                Success = true,
                Data = null,
                Message = "Không có báo cáo trong kì này."
            });
        }

        return Ok(new ApiResponse<List<SubTaskProgressReviewDto>>
        {
            Success = true,
            Data = items,
            Message = "Rà soát tiến độ task con thành công."
        });
    }

}


