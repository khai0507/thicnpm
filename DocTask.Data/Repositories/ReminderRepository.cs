using System.Linq;
using DocTask.Core.Dtos.Reminders;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Paginations;
using Microsoft.EntityFrameworkCore;
using ReminderModel = DocTask.Core.Models.Reminder;

namespace DocTask.Data.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly ApplicationDbContext _context;
    public ReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }



    public async Task<ReminderModel> CreateAsync(ReminderDto dto)
    {
        var entity = new ReminderModel
        {
            Taskid = dto.Taskid,
            Periodid = dto.Periodid,
            Title = dto.Title,
            Message = dto.Message,
            Triggertime = dto.Triggertime,
            Isauto = dto.Isauto,
            Createdby = dto.Createdby,
            Createdat = DateTime.UtcNow,
            Isnotified = dto.Isnotified ?? false,
            Notifiedat = dto.Notifiedat,
            Notificationid = dto.Notificationid,
            UserId = dto.UserId
        };
        _context.Reminders.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<PaginatedList<ReminderDto>> GetAsync(PageOptionsRequest pageOptions, int? taskId = null, int? userId = null, bool? isNotified = null)
    {
        var query = _context.Reminders.OrderByDescending(r =>r.Createdat).AsQueryable();
        if (taskId.HasValue) query = query.Where(r => r.Taskid == taskId.Value);
        if (userId.HasValue) query = query.Where(r => r.UserId == userId.Value);
        if (isNotified.HasValue) query = query.Where(r => r.Isnotified == isNotified.Value);

        var projected = query.Select(r => new ReminderDto
        {
            Reminderid = r.Reminderid,
            Taskid = r.Taskid,
            Periodid = r.Periodid,
            Title = r.Title,
            Message = r.Message,
            Triggertime = r.Triggertime,
            Isauto = r.Isauto,
            Createdby = r.Createdby,
            Createdat = r.Createdat,
            Isnotified = r.Isnotified,
            Notifiedat = r.Notifiedat,
            Notificationid = r.Notificationid,
            UserId = r.UserId
        });

        return await projected.ToPaginatedListAsync(pageOptions);
    }

    public async Task MarkNotifiedAsync(int reminderId)
    {
        var entity = await _context.Reminders.FirstOrDefaultAsync(r => r.Reminderid == reminderId);
        if (entity == null) return;
        entity.Isnotified = true;
        entity.Notifiedat = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<object>> GetUserRemindersAsync(int userId)
    {
        return await _context.Reminders
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Include(r => r.Task)
            .Include(r => r.Period)
            .Include(r => r.Notification)
            .Select(r => new
            {
                r.Reminderid,
                r.Title,
                r.Message,
                r.Isnotified,
                Task = new
                {
                    r.Task.TaskId,
                    r.Task.Title,
                    r.Task.Description,
                    r.Task.Status,
                    r.Task.StartDate,
                    r.Task.DueDate
                },
                Period = r.Period != null ? new
                {
                    r.Period.PeriodId,
                    r.Period.PeriodName
                } : null,
                Notification = r.Notification != null ? new
                {
                    r.Notification.NotificationId,
                    r.Notification.Message,
                    r.Notification.IsRead,
                    r.Notification.CreatedAt
                } : null
            })
            .ToListAsync();
    }

    public async Task<ReminderModel?> GetByIdAsync(int reminderId)
    {
        return await _context.Reminders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Reminderid == reminderId);
    }

    public async Task<bool> DeleteAsync(int reminderId)
    {
        var entity = await _context.Reminders
            .FirstOrDefaultAsync(r => r.Reminderid == reminderId);

        if (entity == null) return false;

        _context.Reminders.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ReminderModel> CreateReminderAsync(int taskId, int userId, string message)
    {
        var reminder = new ReminderModel
        {
            Taskid = taskId,
            Message = message,
            UserId = userId,
            Triggertime = DateTime.Now,
            Createdby = userId,
            Createdat = DateTime.Now,
            Title = message,
            Isauto = false,
            Isnotified = false
        };

        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<int> DeleteByTaskIdAsync(int taskId)
    {
        var reminders = await _context.Reminders
            .Where(r => r.Taskid == taskId)
            .ToListAsync();

        if (reminders.Count == 0) return 0;

        _context.Reminders.RemoveRange(reminders);
        return await _context.SaveChangesAsync();
    }

    public async Task<List<ReminderModel>> GetDueRemindersAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Reminders
            .Where(r => ( r.Isnotified == false || r.Isnotified == null)&& r.Triggertime <= now)
            .ToListAsync();
    }

    public async Task CreateReminder1Async(ReminderModel reminder)
    {
        await _context.Reminders.AddAsync(reminder);
        await _context.SaveChangesAsync();
    }
}



