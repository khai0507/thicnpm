using DocTask.Core.Dtos.SubTasks;
using DocTask.Core.Exceptions;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Models;
using DocTask.Core.Paginations;
using DocTask.Service.Mappers;
using System.Threading.Channels;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using TaskEntity = DocTask.Core.Models.Task;

namespace DocTask.Service.Services;

public class SubTaskService : ISubTaskService
{
    private readonly ISubTaskRepository _subTaskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFrequencyDetailRepository _frequencyDetailRepository;
    private readonly IFrequencyRepository _frequencyRepository;
    private readonly IReminderService _reminderService;

    public SubTaskService(ISubTaskRepository subTaskRepository, ITaskRepository taskRepository, IUserRepository userRepository, IFrequencyDetailRepository frequencyDetailRepository, IFrequencyRepository frequencyRepository, IReminderService reminderService)
    {
        _subTaskRepository = subTaskRepository;
        _userRepository = userRepository;
        _frequencyDetailRepository = frequencyDetailRepository;
        _frequencyRepository = frequencyRepository;
        _reminderService = reminderService;
    }
    
    public async Task<SubTaskDto> CreateAsync(int parentTaskId, CreateSubTaskRequest request, int? userId)
    {
        if (request.StartDate.Value.Day < DateTime.Now.Day || request.StartDate > request.DueDate)
            throw new BadRequestException("StartDate must be earlier than or equal to DueDate.");
        
        // Validate that assignedUserIds only contains users from assignable users list
        if (request.AssignedUserIds != null && request.AssignedUserIds.Any())
        {
            var (subordinates, peers) = await _userRepository.GetSubordinatesAndPeersAsync(userId!.Value);
            var assignableUserIds = subordinates.Select(s => s.UserId).Concat(peers.Select(p => p.UserId)).ToList();

            var invalidUserIds = request.AssignedUserIds.Except(assignableUserIds).ToList();
            if (invalidUserIds.Any())
                throw new BadRequestException("Invalid sub-task assignment");
        }

        // Xác thực Task cha có không ?
        var parentTask = await _subTaskRepository.GetByIdAsync(parentTaskId);
        if (parentTask == null || parentTask.AssignerId != userId)
            throw new ArgumentException("Invalid request");


        // Check days
        if (request.Days.Any(day => day < 1 || request.Frequency.Equals("weekly") && day > 7 || request.Frequency.Equals("monthly") && day > 30))
        {
            throw new BadRequestException("Invalid days");
        }

        // create frequence
        var persistedFrequency = await _frequencyRepository.CreateAsync(new Frequency
        {
            FrequencyType = request.Frequency,
            IntervalValue = request.IntervalValue,
        });

        foreach (var day in request.Days)
        {
            var frequencyDetail = new FrequencyDetail
            {
                FrequencyId = persistedFrequency.FrequencyId,
                DayOfMonth = request.Frequency == "monthly" ? day : null,
                DayOfWeek = request.Frequency == "weekly" ? day : null,
            };
            await _frequencyDetailRepository.CreateAsync(frequencyDetail);
        }

        //Tạo Subtask
        var subTaskEntity = SubTaskMapper.ToEntity(request, userId);
        subTaskEntity.AssignerId = userId;
        subTaskEntity.ParentTaskId = parentTaskId;
        subTaskEntity.FrequencyId = persistedFrequency.FrequencyId;
        foreach (var id in request.AssignedUserIds!)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null)
            {
                subTaskEntity.Users.Add(user);
            }
        }

        var createdSubTask = await _subTaskRepository.CreateAsync(subTaskEntity);

        // Fetch the created task with assigned users to get the complete data
        var taskWithUsers = await _subTaskRepository.GetByIdAsync(createdSubTask.TaskId);
        if (taskWithUsers == null)
            throw new InvalidOperationException("Failed to retrieve created subtask");

        // Tạo nhắc nhở cho tất cả người được gán (dùng for để hỗ trợ nhiều người dùng)
        var createdUsers = taskWithUsers.Users.ToList();
        //for (int i = 0; i < createdUsers.Count; i++)
        //{
        //    var user = createdUsers[i];
        //    var message = $"Subtask created: {taskWithUsers.Title}";
        //    if (message.Length > 255) message = message.Substring(0, 255);
        //    try
        //    {
        //        await _reminderService.CreateReminderAsync(taskWithUsers.TaskId, user.UserId, message);
        //    }
        //    catch
        //    {
        //        // Ignore reminder failures to avoid failing main operation
        //    }
        //}
        await CreateAssignmentRemindersAsync(taskWithUsers, "Bạn đã được giao một công việc mới");


        // Convert entity to DTO to avoid circular references
        return SubTaskMapper.ToSubTaskDto(taskWithUsers);
    }
    // Cập nhật
    public async Task<SubTaskDto?> UpdateSubtask(int subTaskId, UpdateSubTaskRequest request)
    {
        if (request.StartDate.Value.Day < DateTime.Now.Day || request.StartDate > request.DueDate)
            throw new BadRequestException("StartDate must be earlier than or equal to DueDate.");
        
        var existingSubTask = await _subTaskRepository.GetByIdAsync(subTaskId);
        if (existingSubTask == null) return null;

        // Update basic subtask properties
        SubTaskMapper.UpdateEntity(existingSubTask, request);
        var updatedSubtask = await _subTaskRepository.UpdateSubtask(subTaskId, existingSubTask);

        // Handle Frequency update if provided :/
        if (!string.IsNullOrEmpty(request.Frequency) && request.Days != null && request.Days.Count > 0)
        {
            // Validate
            if (request.Days.Any(day => day < 1 ||
                (request.Frequency.Equals("weekly") && day > 7) ||
                (request.Frequency.Equals("monthly") && day > 30)))
            {
                throw new BadRequestException("Invalid days");
            }

            Frequency? frequency;

            // cap nhat va tao moi
            if (existingSubTask.FrequencyId.HasValue)
            {
                frequency = await _frequencyRepository.GetByIdAsync(existingSubTask.FrequencyId.Value);
                if (frequency != null)
                {
                    // update co san freq
                    frequency.FrequencyType = request.Frequency;
                    frequency.IntervalValue = request.IntervalValue != 0 ? request.IntervalValue : frequency.IntervalValue;
                    await _frequencyRepository.UpdateFreAsync(frequency);
                }
                else
                {
                    // tao moi neu interval ko co
                    frequency = await _frequencyRepository.CreateAsync(new Frequency
                    {
                        FrequencyType = request.Frequency,
                        IntervalValue = request.IntervalValue != 0 ? request.IntervalValue : 1,
                    });
                    existingSubTask.FrequencyId = frequency.FrequencyId;
                }
            }
            else
            {
                // tao freq
                frequency = await _frequencyRepository.CreateAsync(new Frequency
                {
                    FrequencyType = request.Frequency,
                    IntervalValue = request.IntervalValue != 0 ? request.IntervalValue : 1,
                });
                existingSubTask.FrequencyId = frequency.FrequencyId;
            }

            // xoa freq cu neu daily va tao moi freq detail
            await _frequencyDetailRepository.DeleteByFrequencyIdAsync(frequency!.FrequencyId);

            foreach (var day in request.Days)
            {
                await _frequencyDetailRepository.CreateAsync(new FrequencyDetail
                {
                    FrequencyId = frequency!.FrequencyId,
                    DayOfMonth = request.Frequency == "monthly" ? day : null,
                    DayOfWeek = request.Frequency == "weekly" ? day : null,
                });
            }
        }

        // Handle user assignments if provided
        if (request.AssignedUserIds != null && updatedSubtask != null)
        {
            await _subTaskRepository.AssignUsersToTaskAsync(updatedSubtask.TaskId, request.AssignedUserIds);
            updatedSubtask = await _subTaskRepository.GetByIdAsync(updatedSubtask.TaskId);
        }

        // Create reminders for current assigned users to notify changes (dùng for)
        if (updatedSubtask != null)
        {
            //var updatedUsers = updatedSubtask.Users.ToList();
            //for (int i = 0; i < updatedUsers.Count; i++)
            //{
            //    var user = updatedUsers[i];
            //    var message = $"Subtask updated: {updatedSubtask.Title}";
            //    if (message.Length > 255) message = message.Substring(0, 255);
            //    try
            //    {
            //        await _reminderService.CreateReminderAsync(updatedSubtask.TaskId, user.UserId, message);
            //    }
            //    catch
            //    {
            //        // Ignore reminder failures to avoid failing main operation
            //    }
            //}
            await UpdateAssigmentReminderAsync(existingSubTask, updatedSubtask);
        }

        return updatedSubtask != null ? SubTaskMapper.ToSubTaskDto(updatedSubtask) : null;
    }
    // Xóa
    public async Task<bool> DeleteAsync(int subTaskId, int userId)
    {
        var existingSubTask = await _subTaskRepository.GetByIdAsync(subTaskId);
        if (existingSubTask == null)
            throw new NotFoundException("Sub-task không tồn tại");
        if (existingSubTask.ParentTaskId == null)
            throw new BadRequestException("Không dược xóa task cha");

        if (existingSubTask.AssignerId == null || existingSubTask.AssignerId != userId)
            throw new UnauthorizedException("Bạn không có quyền xóa sub-task này");

        return await _subTaskRepository.DeleteAsync(subTaskId);
    }

    public async Task<PaginatedList<SubTaskDto>> GetAllByParentIdAsync(int parentTaskId, PageOptionsRequest pageOptions, string? key)
    {
        var paginatedSubTasks = await _subTaskRepository.GetAllByParentIdPaginatedAsync(parentTaskId, pageOptions, key);
        var dtoList = paginatedSubTasks.Items.Select(SubTaskMapper.ToSubTaskDto).ToList();

        return new PaginatedList<SubTaskDto>(dtoList, paginatedSubTasks.MetaData);
    }
    
    public async Task<PaginatedList<SubTaskDto>> GetByAssignedUserIdPaginatedAsync(int userId, string? key, PageOptionsRequest pageOptions)
    {
        var paginatedSubTasks = await _subTaskRepository.GetByAssignedUserIdPaginatedAsync(userId, key, pageOptions);
        var dtoList = paginatedSubTasks.Items.Select(SubTaskMapper.ToSubTaskDto).ToList();

        return new PaginatedList<SubTaskDto>(dtoList, paginatedSubTasks.MetaData);
    }
    
    public async Task<List<SubTaskDto>> GetByKeywordAsync(string keyword)
    {
        var subTasks = await _subTaskRepository.GetByKeywordAsync(keyword);
        return subTasks.Select(SubTaskMapper.ToSubTaskDto).ToList();
    }

    public async Task AssignUsersToTaskAsync(int taskId, List<int> userIds)
    {
        await _subTaskRepository.AssignUsersToTaskAsync(taskId, userIds);
    }

    public async Task<List<int>> GetAssignedUserIdsAsync(int taskId)
    {
        return await _subTaskRepository.GetAssignedUserIdsAsync(taskId);
    }

    public async Task RemoveUserFromTaskAsync(int taskId, int userId)
    {
        await _subTaskRepository.RemoveUserFromTaskAsync(taskId, userId);
    }

    private async Task CreateAssignmentRemindersAsync(TaskEntity task, string baseMessage)
    {
        var assignedUsers = task.Users.ToList();
        for (int i = 0; i < assignedUsers.Count; i++)
        {
            var user = assignedUsers[i];
            var message = $"{baseMessage}: {task.Title}";
            if (message.Length > 255) message = message.Substring(0, 255);

            try
            {
                // Tạo reminder trong database
                await _reminderService.CreateReminderAsync(task.TaskId, user.UserId, message);

                // Gửi thông báo real-time qua SignalR
                await _reminderService.SendRealTimeNotificationAsync(
                    user.UserId,
                    "Bạn vừa nhận được công việc mới",
                    message,
                    new { TaskId = task.TaskId, TaskTitle = task.Title, task.StartDate, task.DueDate }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create reminder for user {user.UserId}: {ex.Message}");
            }
        }
    }

    private async Task UpdateAssigmentReminderAsync(TaskEntity oldTask, TaskEntity newTask)
    {
        var updateChanges = DetectChanges(oldTask, newTask);
        if (updateChanges.Count == 0) return; // No changes detected

        var updateAssignedUsers = newTask.Users.ToList();
        foreach (var user in updateAssignedUsers)
        {
            var message = $"Công việc: {newTask.Title} đã được cập nhật: {string.Join(", ", updateChanges)}";
            if (message.Length > 255) message = message.Substring(0, 255);

            try
            {
                await _reminderService.CreateReminderAsync(newTask.TaskId, user.UserId, message);

                await _reminderService.SendRealTimeNotificationAsync(
                    user.UserId,
                    "Công việc được cập nhật",
                    message,
                    new
                    {
                        TaskId = newTask.TaskId,
                        TaskTitle = newTask.Title,
                        newTask.StartDate,
                        newTask.DueDate,
                        Frequency = newTask.Frequency != null
                            ? new { newTask.Frequency.FrequencyId, newTask.Frequency.FrequencyType, newTask.Frequency.IntervalValue }
                            : null
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create reminder for user {user.UserId}: {ex.Message}");
            }
        }

    }

    private List<String> DetectChanges (TaskEntity oldTask, TaskEntity newTask)
    {
        var changes = new List<string>();
        if (oldTask.Title != newTask.Title)
            changes.Add("Title");
        if (oldTask.Description != newTask.Description)
            changes.Add("Description");
        if (oldTask.StartDate != newTask.StartDate)
            changes.Add("StartDate");
        if (oldTask.DueDate != newTask.DueDate)
            changes.Add("DueDate");
        if (oldTask.Status != newTask.Status)
            changes.Add("Status");
        if (oldTask.Priority != newTask.Priority)
            changes.Add("Priority");
        if (oldTask.Percentagecomplete != newTask.Percentagecomplete)
            changes.Add("Percentagecomplete");
        if (oldTask.FrequencyId != newTask.FrequencyId)
            changes.Add("Frequency");
        return changes;
    }
}