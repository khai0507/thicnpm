// using System.Security.Claims;
// using DocTask.Core.DTOs.ApiResponses;
// using DocTask.Core.DTOs.Reminders;
// using DocTask.Core.Exceptions;
// using DocTask.Core.Interfaces.Services;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using DocTask.Core.Paginations;
// using DocTask.Core.Dtos.Reminders;

// namespace DocTask.Api.Controllers;

// [ApiController]
// [Route("/api/v1/reminder")]
// public class ReminderController : ControllerBase
// {
//     private readonly IReminderService _reminderService;

//     public ReminderController(IReminderService reminderService)
//     {
//         _reminderService = reminderService;
//     }

//     // GET: /api/v1/reminder?page=1&size=10&taskId=&userId=&isNotified=
//     [HttpGet]
//     [Authorize]
//     public async Task<IActionResult> GetPaginatedAsync(
//         [FromQuery] int page = 1,
//         [FromQuery] int size = 10,
//         [FromQuery] int? taskId = null,
//         [FromQuery] int? userId = null,
//         [FromQuery] bool? isNotified = null)
//     {
        
//         var pageOptions = new PageOptionsRequest { Page = page, Size = size };
//         var reminders = await _reminderService.GetAsync(pageOptions, taskId, userId, isNotified);

//         return Ok(new ApiResponse<PaginatedList<ReminderDto>>
//         {
//             Data = reminders,
//             Message = "Lấy danh sách nhắc nhở có phân trang thành công.",
//             Success = true
//         });
//     }

//     [HttpGet("user-reminders")]
//     public async Task<IActionResult> GetUserRemindersAsync()
//     {
//         var username = GetUsernameFromHttpContext();

//         if (string.IsNullOrEmpty(username))
//         {
//             throw new UnauthorizedException("Không thể xác thực người dùng.");
//         }

//         var reminders = await _reminderService.GetUserRemindersByUsernameAsync(username);

//         return Ok(new ApiResponse<object>
//         {
//             Data = reminders,
//             Message = "Lấy danh sách nhắc nhở thành công."
//         });
//     }

//     [HttpPost("create/{taskId}/{userId}")]
//     [Authorize(Roles = "Admin, User")]
//     public async Task<IActionResult> CreateReminderAsync(int taskId, int userId, [FromBody] CreateReminderRequestDto request)
//     {
//         var reminder = await _reminderService.CreateReminderAsync(taskId, userId, request.Message);

//         return Ok(new ApiResponse<object>
//         {
//             Data = new
//             {
//                 reminder.Reminderid,
//                 reminder.Title,
//                 reminder.Message,
//                 reminder.Triggertime,
//                 reminder.Createdat,
//                 TaskId = reminder.Taskid,
//                 UserId = reminder.UserId
//             },
//             Message = "Tạo nhắc nhở thành công."
//         });
//     }

//     [HttpDelete("delete/{reminderId}")]
//     [Authorize(Roles = "Admin, User")]
//     public async Task<IActionResult> DeleteReminderAsync(int reminderId)
//     {
//         var result = await _reminderService.DeleteReminderAsync(reminderId);

//         return Ok(new ApiResponse<object>
//         {
//             Data = new
//             {
//                 reminderId = reminderId,
//                 message = "Nhắc nhở đã được xóa thành công."
//             },
//             Message = "Xóa nhắc nhở thành công."
//         });
//     }
//     private string? GetUsernameFromHttpContext()
//     {
//         var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
//         return claim?.Value;
//     }
// }
using System.Security.Claims;
using DocTask.Core.DTOs.ApiResponses;
using DocTask.Core.DTOs.Reminders;
using DocTask.Core.Exceptions;
using DocTask.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocTask.Core.Paginations;
using DocTask.Core.Dtos.Reminders;
using Microsoft.AspNetCore.SignalR;
using DocTask.Api.Providers;

namespace DocTask.Api.Controllers;

[ApiController]
[Route("/api/v1/reminder")]
public class ReminderController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ReminderController(
        IReminderService reminderService,
        IHubContext<NotificationHub> hubContext)
    {
        _reminderService = reminderService;
        _hubContext = hubContext;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetPaginatedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] int? taskId = null,
        [FromQuery] int? userId = null,
        [FromQuery] bool? isNotified = null)
    {
        var pageOptions = new PageOptionsRequest { Page = page, Size = size };
        var reminders = await _reminderService.GetAsync(pageOptions, taskId, userId, isNotified);

        return Ok(new ApiResponse<PaginatedList<ReminderDto>>
        {
            Data = reminders,
            Message = "Lấy danh sách nhắc nhở có phân trang thành công.",
            Success = true
        });
    }

    [HttpGet("user-reminders")]
    [Authorize]
    public async Task<IActionResult> GetUserRemindersAsync()
    {
        var username = GetUsernameFromHttpContext();

        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Không thể xác thực người dùng.",
                Data = null
            });
        }

        var reminders = await _reminderService.GetUserRemindersByUsernameAsync(username);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Lấy danh sách nhắc nhở thành công.",
            Data = reminders
        });
    }

    [HttpPost("create/{taskId}/{userId}")]
    [Authorize(Roles = "Admin, User")]
    public async Task<IActionResult> CreateReminderAsync(int taskId, int userId, [FromBody] CreateReminderRequestDto request)
    {
        var reminder = await _reminderService.CreateReminderAsync(taskId, userId, request.Message);

        // Gửi thông báo realtime qua SignalR
        await _hubContext.Clients.Group($"user-{userId}")
            .SendAsync("ReceiveNotification", new
            {
                title = "Nhắc nhở mới",
                message = reminder.Message,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                data = new
                {
                    reminder.Reminderid,
                    reminder.Title,
                    reminder.Message,
                    reminder.Triggertime,
                    reminder.Createdat,
                    TaskId = reminder.Taskid,
                    UserId = reminder.UserId
                }
            });

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Tạo nhắc nhở thành công.",
            Data = new
            {
                reminder.Reminderid,
                reminder.Title,
                reminder.Message,
                reminder.Triggertime,
                reminder.Createdat,
                TaskId = reminder.Taskid,
                UserId = reminder.UserId
            }
        });
    }

    [HttpDelete("delete/{reminderId}")]
    [Authorize(Roles = "Admin, User")]
    public async Task<IActionResult> DeleteReminderAsync(int reminderId)
    {
        var result = await _reminderService.DeleteReminderAsync(reminderId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Xóa nhắc nhở thành công.",
            Data = new
            {
                reminderId,
                message = "Nhắc nhở đã được xóa thành công."
            }
        });
    }

    private string? GetUsernameFromHttpContext()
    {
        var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }
}