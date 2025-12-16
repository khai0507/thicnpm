using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Exceptions;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Paginations;
using Microsoft.EntityFrameworkCore;
using TaskModel = DocTask.Core.Models.Task;

namespace DocTask.Data.Repositories;

public class TaskRepository : ITaskRepository
{
  private readonly ApplicationDbContext _context;
  private readonly ISubTaskRepository _subTaskRepository;

  public TaskRepository(ApplicationDbContext context, ISubTaskRepository subTaskRepository)
  {
    _context = context;
    _subTaskRepository = subTaskRepository;
  }

  public async Task<PaginatedList<TaskModel>> GetAllAsync(PageOptionsRequest pageOptions, string? key, int userId)
  {
    var query = _context.Tasks.Where(t => t.ParentTaskId == null && t.AssignerId == userId && t.IsDeleted == false)
      .OrderByDescending(t => t.CreatedAt).AsQueryable();
    if (key != null)
      query = query.Where(t => t.Title.StartsWith(key));
                                                                                                                                
    return await query.ToPaginatedListAsync(pageOptions);
  }


  public async Task<TaskModel?> GetTaskByIdAsync(int taskId)
  {
    return await _context.Tasks
        .Include(t => t.Assignee)
        .Include(t => t.Frequency)
        .ThenInclude(f => f.FrequencyDetails)
        .FirstOrDefaultAsync(t => t.TaskId == taskId && t.IsDeleted == false);
  }

  public async Task<TaskModel?> CreateTaskAsync(TaskModel task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

  

  public async Task<TaskModel?> UpdateTaskAsync(int taskId, UpdateTaskDto taskDto)
  {
    var existingTask = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.TaskId == taskId && t.IsDeleted == false);
      if (existingTask == null)
        return null;

      // Cập nhật thông tin cơ bản
      existingTask.Title = taskDto.Title;
      existingTask.Description = taskDto.Description;
      existingTask.StartDate = taskDto.StartDate;
      existingTask.DueDate = taskDto.DueDate;

      _context.Tasks.Update(existingTask);
      await _context.SaveChangesAsync();

      return existingTask;
  }
  
  public async Task<bool> DeleteAsync(TaskModel task)
  {
      using (var transaction = await _context.Database.BeginTransactionAsync())
      {
          try
          {
            var foundTask = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == task.TaskId && t.IsDeleted == false);
            await _context.Tasks
              .Where(e => e.ParentTaskId == foundTask.TaskId)
              .ExecuteUpdateAsync(setter => setter.SetProperty(e => e.IsDeleted, true));
            
            foundTask.IsDeleted = true;
            _context.Tasks.Update(foundTask);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
          }
          catch (Exception)
          {
            await transaction.RollbackAsync();
            throw new BadRequestException("Error in execute command");
          }
      }

      return true;
  }

  public async Task<bool> CreateTaskUnitAssignmentAsync(int taskId, int unitId)
  {
      var taskUnitAssignment = new Core.Models.Taskunitassignment
      {
          TaskId = taskId,
          UnitId = unitId
      };
      
      _context.Taskunitassignments.Add(taskUnitAssignment);
      await _context.SaveChangesAsync();
      return true;
  }
}

