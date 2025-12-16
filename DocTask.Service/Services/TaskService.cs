using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Exceptions;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Paginations;
using DocTask.Service.Mappers;
using TaskModel = DocTask.Core.Models.Task;

namespace DocTask.Service.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IReminderService _reminderService;
    private readonly IUserRepository _userRepository;
    private readonly ISubTaskRepository _subTaskRepository;

    public TaskService(ITaskRepository taskRepository, IReminderService _reminderService, IUserRepository userRepository, ISubTaskRepository subTaskRepository)
    {
        _taskRepository = taskRepository;
        _subTaskRepository = subTaskRepository;
        _reminderService = _reminderService;
        
    }

    public async Task<PaginatedList<TaskDto>> GetAll(PageOptionsRequest pageOptions, string? key, int userId)
    {
        var paginatedListModel = await _taskRepository.GetAllAsync(pageOptions, key, userId);

        return new PaginatedList<TaskDto>
        {
            MetaData = paginatedListModel.MetaData,
            Items = paginatedListModel.Items.Select(t => t.ToTaskDto()).ToList(),
        };
    }

    public async Task<TaskDto?> CreateTaskAsync(CreateTaskDto taskDto, int userId)
    {
        if (taskDto.StartDate.Value.Day < DateTime.Now.Day || taskDto.StartDate > taskDto.DueDate)
            throw new BadRequestException("StartDate must be earlier than or equal to DueDate.");
        
        var task = new TaskModel
        {
            Title = taskDto.Title,
            Description = taskDto.Description,
            AssignerId = userId, // server tự gán
            CreatedAt = DateTime.UtcNow,
            StartDate = taskDto.StartDate,
            DueDate = taskDto.DueDate,
        };

        var created = await _taskRepository.CreateTaskAsync(task);

        return created.ToTaskDto();
    }

    public async Task<TaskDto> UpdateTaskAsync(int taskId, UpdateTaskDto taskDto, int userId)
    {
        if (taskDto.StartDate.Value.Day < DateTime.Now.Day || taskDto.StartDate > taskDto.DueDate)
            throw new BadRequestException("StartDate must be earlier than or equal to DueDate.");
        
        var existingTask = await _taskRepository.GetTaskByIdAsync(taskId);
        if (existingTask == null)
        {
            throw new NotFoundException($"Không tìm thấy task với ID {taskId}.");
        }
        if (existingTask.AssignerId != userId)
        {
            throw new UnauthorizedException("Bạn không có quyền cập nhật task này.");
        }
        var updated = await _taskRepository.UpdateTaskAsync(taskId, taskDto);
        if (updated == null)
        {
            throw new InternalServerErrorException("Cập nhật task thất bại.");
        }

        return updated.ToTaskDto();
    }

    public async Task DeleteTaskAsync(int taskId, int userId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
        if (task == null)
            throw new NotFoundException($"Invalid task");
        
        if (task.ParentTaskId != null)
            throw new ConflictException("Invalid task");
        
        // Kiểm tra quyền
        if (task.AssignerId != userId && task.AssigneeId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa task này.");
        await _taskRepository.DeleteAsync(task);
    }
}