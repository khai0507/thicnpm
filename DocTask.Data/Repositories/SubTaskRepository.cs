using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Models;
using DocTask.Core.Paginations;
using Microsoft.EntityFrameworkCore;
using TaskEntity = DocTask.Core.Models.Task;
using Task = System.Threading.Tasks.Task;

namespace DocTask.Data.Repositories
{
    public class SubTaskRepository : ISubTaskRepository
    {
        private readonly ApplicationDbContext _context;

        public SubTaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TaskEntity?> GetByIdAsync(int subTaskId)
        {
            return await _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(t => t.TaskId == subTaskId && t.IsDeleted == false);
        }
        public async Task<TaskEntity?> GetBySubIdAsync(int parentTaskId, int subTaskId)
        {
            return await _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .FirstOrDefaultAsync(t => t.TaskId == subTaskId && t.ParentTaskId == parentTaskId && t.IsDeleted == false);
        }

        public async Task<TaskEntity> CreateAsync(TaskEntity subTask)
        {
            subTask.CreatedAt = DateTime.UtcNow;
            _context.Tasks.Add(subTask);
            await _context.SaveChangesAsync();
            return subTask;
        }
        //Tu dong
        public async Task<TaskEntity?> UpdateSubtask(int subTaskId, TaskEntity subtask)
        {
            var existingSubtask = await _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .FirstOrDefaultAsync(t => t.TaskId == subtask.TaskId && t.IsDeleted == false);
            if (existingSubtask == null)
                return null;

            // Update basic properties
            existingSubtask.Title = subtask.Title;
            existingSubtask.Description = subtask.Description;
            existingSubtask.StartDate = subtask.StartDate;
            existingSubtask.DueDate = subtask.DueDate;

            // The Users collection is already updated in the service layer
            // Entity Framework will track the changes automatically

            await _context.SaveChangesAsync();
            return existingSubtask;
        }

        public async Task<bool> DeleteAsync(int subTaskId)
        {
            var subTask = await GetByIdAsync(subTaskId);
            if (subTask == null)
                return false;

            subTask.IsDeleted = true;
            _context.Tasks.Update(subTask);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int subTaskId)
        {
            return await _context.Tasks
                .AnyAsync(t => t.TaskId == subTaskId && t.ParentTaskId != null);
        }

        public async Task<List<TaskEntity>> GetAllByParentIdAsync(int parentTaskId)
        {
            return await _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .Where(t => t.ParentTaskId == parentTaskId && t.IsDeleted == false)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaginatedList<TaskEntity>> GetAllByParentIdPaginatedAsync(int parentTaskId, PageOptionsRequest pageOptions, string? key)
        {
            var query = _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .Where(t => t.ParentTaskId == parentTaskId && t.IsDeleted == false)
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            if (key != null)
                query = query.Where(t => t.Title.StartsWith(key));
            
            return await query.ToPaginatedListAsync(pageOptions);
        }

        public async Task<List<TaskEntity>> GetByAssigneeIdAsync(int assigneeId)
        {
            return await _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .Where(t => t.AssigneeId == assigneeId && t.ParentTaskId != null && t.IsDeleted == false)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<PaginatedList<TaskEntity>> GetByAssigneeIdPaginatedAsync(int assigneeId, PageOptionsRequest pageOptions)
        {
            var query = _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .Where(t => t.AssigneeId == assigneeId && t.ParentTaskId != null && t.IsDeleted == false)
                .OrderBy(t => t.DueDate);

            return await query.ToPaginatedListAsync(pageOptions);
        }

        public async Task<List<TaskEntity>> GetByAssignedUserIdAsync(int userId)
        {
            return await _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .Where(t => t.ParentTaskId != null && t.Users.Any(u => u.UserId == userId) && t.IsDeleted == false)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<PaginatedList<TaskEntity>> GetByAssignedUserIdPaginatedAsync(int userId, string? key, PageOptionsRequest pageOptions)
        {
            var query = _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .Where(t => t.ParentTaskId != null && t.Users.Any(u => u.UserId == userId) && t.IsDeleted == false)
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            if (key != null)
                query = query.Where(t => t.Title.StartsWith(key));

            return await query.ToPaginatedListAsync(pageOptions);
        }

        public async Task<List<TaskEntity>> GetByKeywordAsync(string keyword)
        {
            return await _context.Tasks
                .Include(t => t.Users)
                .Include(t => t.Frequency!)
                    .ThenInclude(f => f.FrequencyDetails)
                .AsNoTracking()
                .Where(t => t.ParentTaskId != null &&
                           (t.Title.Contains(keyword) ||
                            (t.Description != null && t.Description.Contains(keyword))) && t.IsDeleted == false)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AssignUsersToTaskAsync(int taskId, List<int> userIds)
        {
            var task = await _context.Tasks
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.IsDeleted == false);

            if (task == null) return;

            // Clear all existing user assignments
            task.Users.Clear();

            // Add new user assignments
            if (userIds.Any())
            {
                var usersToAdd = await _context.Users
                    .Where(u => userIds.Contains(u.UserId))
                    .ToListAsync();

                foreach (var user in usersToAdd)
                {
                    task.Users.Add(user);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> GetAssignedUserIdsAsync(int taskId)
        {
            return await _context.Tasks
                .Where(t => t.TaskId == taskId && t.IsDeleted == false)
                .SelectMany(t => t.Users.Select(u => u.UserId))
                .ToListAsync();
        }

        public async Task RemoveUserFromTaskAsync(int taskId, int userId)
        {
            var task = await _context.Tasks
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.IsDeleted == false);

            if (task == null) return;

            var userToRemove = task.Users.FirstOrDefault(u => u.UserId == userId);
            if (userToRemove != null)
            {
                task.Users.Remove(userToRemove);
                await _context.SaveChangesAsync();
            }
        }

    }
}