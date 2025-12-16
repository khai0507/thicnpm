using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Paginations;
using TaskModel = DocTask.Core.Models.Task;

namespace DocTask.Core.Interfaces.Repositories;

public interface ITaskRepository
{
    // tasks
    Task<PaginatedList<TaskModel>> GetAllAsync(PageOptionsRequest pageOptions, string? key, int userId);
    Task<TaskModel?> GetTaskByIdAsync(int taskId);
    Task<TaskModel?> CreateTaskAsync(TaskModel task);
    Task<TaskModel?> UpdateTaskAsync(int taskId, UpdateTaskDto taskDto);
    Task<bool>  DeleteAsync(TaskModel task);
    Task<bool> CreateTaskUnitAssignmentAsync(int taskId, int unitId);
}