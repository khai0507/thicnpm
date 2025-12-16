using DocTask.Core.Dtos.Reminders;
using DocTask.Core.Paginations;
using ReminderModel = DocTask.Core.Models.Reminder;

namespace DocTask.Core.Interfaces.Repositories;

public interface IReminderRepository
{
    Task<ReminderModel> CreateAsync(ReminderDto dto);
    Task<PaginatedList<ReminderDto>> GetAsync(PageOptionsRequest pageOptions, int? taskId = null, int? userId = null, bool? isNotified = null);
    Task MarkNotifiedAsync(int reminderId);
    Task<IEnumerable<object>> GetUserRemindersAsync(int userId);
    Task<ReminderModel?> GetByIdAsync(int reminderId);
    Task<bool> DeleteAsync(int reminderId);
    Task<ReminderModel> CreateReminderAsync(int taskId, int userId, string message);
    Task<int> DeleteByTaskIdAsync(int taskId);

    Task<List<ReminderModel>> GetDueRemindersAsync();
    Task CreateReminder1Async(ReminderModel reminder);
}


