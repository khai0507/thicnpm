using DocTask.Core.Paginations;
using TaskEntity = DocTask.Core.Models.Task;

namespace DocTask.Core.Interfaces.Repositories;

public interface ISubTaskRepository
{
    // Basic CRUD operations
    Task<TaskEntity?> GetByIdAsync(int subTaskId);
    Task<TaskEntity?> GetBySubIdAsync(int parentTaskId, int subTaskId);
    Task<TaskEntity> CreateAsync(TaskEntity subTask);
    Task<TaskEntity?> UpdateSubtask(int subTaskId, TaskEntity subtask);

    Task<bool> DeleteAsync(int subTaskId);
    Task<bool> ExistsAsync(int subTaskId);

    // Query operations
    Task<List<TaskEntity>> GetAllByParentIdAsync(int parentTaskId);
    Task<PaginatedList<TaskEntity>> GetAllByParentIdPaginatedAsync(int parentTaskId, PageOptionsRequest pageOptions, string? key);
    Task<List<TaskEntity>> GetByAssigneeIdAsync(int assigneeId);
    Task<PaginatedList<TaskEntity>> GetByAssigneeIdPaginatedAsync(int assigneeId, PageOptionsRequest pageOptions);
    Task<List<TaskEntity>> GetByAssignedUserIdAsync(int userId);
    Task<PaginatedList<TaskEntity>> GetByAssignedUserIdPaginatedAsync(int userId, string? key, PageOptionsRequest pageOptions);
    Task<List<TaskEntity>> GetByKeywordAsync(string keyword);

    // Task assignment operations
    Task AssignUsersToTaskAsync(int taskId, List<int> userIds);
    Task<List<int>> GetAssignedUserIdsAsync(int taskId);
    Task RemoveUserFromTaskAsync(int taskId, int userId);
}