using DocTask.Core.Dtos.Reminders;
using DocTask.Core.Paginations;
using ReminderModel = DocTask.Core.Models.Reminder;

namespace DocTask.Core.Interfaces.Services;

public interface IReminderService
{

    //
    Task SendRealTimeNotificationAsync(int userId, string title, string message, object? data = null);
    Task SendMultipleNotificationsAsync(List<int> userIds, string title, string message, object? data = null);
    Task CreateAndUpdateReminderAsync(int subTaskId, int userId, DateTime dueDate);
    Task <ReminderDto> CreateRemider1Async(RemiderRequest request);



    Task<ReminderModel> CreateAsync(ReminderDto dto);
    Task<PaginatedList<ReminderDto>> GetAsync(PageOptionsRequest pageOptions, int? taskId = null, int? userId = null, bool? isNotified = null);
    Task MarkNotifiedAsync(int reminderId);
    Task<IEnumerable<object>> GetUserRemindersAsync(int userId);
    Task<IEnumerable<object>> GetUserRemindersByUsernameAsync(string username);
    Task<ReminderModel> CreateReminderAsync(int taskId, int userId, string message);
    Task<bool> DeleteReminderAsync(int reminderId);
    Task<int> DeleteByTaskIdAsync(int taskId);
}


