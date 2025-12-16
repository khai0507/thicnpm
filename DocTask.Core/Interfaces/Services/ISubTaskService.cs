using DocTask.Core.Dtos.SubTasks;
using DocTask.Core.Models;
using DocTask.Core.Paginations;
using TaskEntity = DocTask.Core.Models.Task;
using Task = System.Threading.Tasks.Task;

namespace DocTask.Core.Interfaces.Services;

public interface ISubTaskService
{
    Task<SubTaskDto> CreateAsync(int parentTaskId, CreateSubTaskRequest request, int? userId);
    Task<SubTaskDto?> UpdateSubtask(int subtaskId, UpdateSubTaskRequest request);
    Task<bool> DeleteAsync(int subTaskId, int userId);

    // Query operations
    Task<PaginatedList<SubTaskDto>> GetAllByParentIdAsync(int parentTaskId, PageOptionsRequest pageOptions, string? key);
    Task<PaginatedList<SubTaskDto>> GetByAssignedUserIdPaginatedAsync(int userId, string? key, PageOptionsRequest pageOptions);
}