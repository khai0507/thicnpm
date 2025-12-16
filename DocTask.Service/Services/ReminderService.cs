using DocTask.Api.Providers;
using DocTask.Core.Dtos.Reminders;
using DocTask.Core.Exceptions;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Paginations;
using DocTask.Data;
using DocTask.Service.Mappers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReminderModel = DocTask.Core.Models.Reminder;

namespace DocTask.Service.Services;

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _repo;
    private readonly IUserRepository _userRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(IReminderRepository repo, IUserRepository userRepository, ApplicationDbContext dbContext,
                       IHubContext<NotificationHub> hubContext, ILogger<ReminderService> logger)
    {
        _repo = repo;
        _userRepository = userRepository;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task<ReminderModel> CreateAsync(ReminderDto dto) => _repo.CreateAsync(dto);
    public async Task<ReminderDto> CreateRemider1Async(RemiderRequest request)
    {
        var remider = ReminderMapper.ToRemider(request);
        await _repo.CreateReminder1Async(remider);
        return ReminderMapper.FromRemider(remider);
    }

    public Task<PaginatedList<ReminderDto>> GetAsync(PageOptionsRequest pageOptions, int? taskId = null, int? userId = null, bool? isNotified = null)
        => _repo.GetAsync(pageOptions, taskId, userId, isNotified);

    public Task MarkNotifiedAsync(int reminderId) => _repo.MarkNotifiedAsync(reminderId);

    public async Task<IEnumerable<object>> GetUserRemindersAsync(int userId)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException("Người dùng không tồn tại.");
        }

        return await _repo.GetUserRemindersAsync(userId);
    }

    public async Task<IEnumerable<object>> GetUserRemindersByUsernameAsync(string username)
    {
        // Get user by username
        var user = await _userRepository.GetByUserNameAsync(username);

        if (user == null)
        {
            throw new NotFoundException("Người dùng không tồn tại.");
        }

        return await _repo.GetUserRemindersAsync(user.UserId);
    }

    public async Task<ReminderModel> CreateReminderAsync(int taskId, int userId, string message)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException("Người dùng không tồn tại.");
        }

        // Validate task exists
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.TaskId == taskId)
            .FirstOrDefaultAsync();

        if (task == null)
        {
            throw new NotFoundException("Nhiệm vụ không tồn tại hoặc không thuộc về người dùng này.");
        }

        return await _repo.CreateReminderAsync(taskId, userId, message);
    }

    public async Task<bool> DeleteReminderAsync(int reminderId)
    {
        // Validate reminder exists
        var reminder = await _repo.GetByIdAsync(reminderId);
        if (reminder == null)
        {
            throw new NotFoundException("Nhắc nhở không tồn tại.");
        }

        return await _repo.DeleteAsync(reminderId);
    }

    public Task<int> DeleteByTaskIdAsync(int taskId)
    {
        return _repo.DeleteByTaskIdAsync(taskId);
    }

    public async Task SendRealTimeNotificationAsync(int userId, string title, string message, object? data = null)
    {
        try
        {
            var notification = new
            {
                Title = title,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Data = data
            };

            await _hubContext.Clients.Group($"user-{userId}")
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation($"Sent notification to user {userId}: {title}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to user {userId}");
        }
    }

    public Task SendMultipleNotificationsAsync(List<int> userIds, string title, string message, object? data = null)
    {
        throw new NotImplementedException();
    }

    public async Task CreateAndUpdateReminderAsync(int subTaskId, int userId, DateTime dueDate)
    {
        await _repo.DeleteByTaskIdAsync(subTaskId);

        var remider = new ReminderModel
        {
            Taskid = subTaskId,
            UserId = userId,
            Triggertime = dueDate.AddDays(-1).Date.AddHours(8),
            Title = "Nhắc nhở nhiệm vụ",
            Message = "Bạn có công việc sắp đến hạn",
            Isnotified = false,
            Createdat = DateTime.Now,
            Isauto = true,
            Createdby = userId
        };

        await _repo.CreateReminder1Async(remider);
    }

    
}







