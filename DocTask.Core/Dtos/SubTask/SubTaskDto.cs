using DocTask.Core.Models;
using System.ComponentModel.DataAnnotations;
using DocTask.Core.Dtos.Users;

namespace DocTask.Core.Dtos.SubTasks
{
    public class SubTaskDto
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int? AssignerId { get; set; }
        public int? AssigneeId { get; set; }
        public int? PeriodId { get; set; }
        public int? AttachedFile { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? FrequencyType { get; set; }
        public int? IntervalValue { get; set; }
        public List<int>? FrequencyDays { get; set; }
        public int? Percentagecomplete { get; set; }
        public int? ParentTaskId { get; set; }
        public List<UserDto>? AssignedUsers { get; set; }
    }

    public class CreateSubTaskRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "StartDate is required")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "DueDate is required")]
        public DateTime? DueDate { get; set; }

        [Required(ErrorMessage = "Frequency is required")]
        public string Frequency { get; set; } = string.Empty;

        [Required(ErrorMessage = "IntervalValue is required")]
        public int IntervalValue { get; set; }

        public List<int> Days { get; set; } = new();

        [Required(ErrorMessage = "AssignedUserIds is required")]
        public List<int>? AssignedUserIds { get; set; } // List of user IDs to assign the task to
    }

    public class UpdateSubTaskRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public int IntervalValue { get; set; }
        public List<int> Days { get; set; } = new();
        public List<int>? AssignedUserIds { get; set; }
    }
}