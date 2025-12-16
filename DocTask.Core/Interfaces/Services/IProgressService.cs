using DocTask.Core.Dtos.Tasks;

namespace DocTask.Core.Interfaces.Services;

public interface IProgressService
{
    Task<UpdateProgressResponse> UpdateProgressAsync(int taskId, UpdateProgressRequest request, int? updatedBy = null);

    Task<List<ProgressDto>> GetProgressesByTaskAsync(int taskId);

    Task<Core.Models.Progress?> GetProgressByIdAsync(int progressId);

    Task<Core.Models.Progress?> UpdateProgressEntryAsync(int progressId, UpdateProgressRequest request, int? updatedBy = null);

    Task<bool> DeleteProgressAsync(int progressId);
    Task<List<ProgressReviewByUserDto>> ReviewProgressByUserAsync(int taskId, DateTime? from, DateTime? to, string? status);
    Task<List<SubTaskProgressReviewDto>> ReviewSubTaskProgressAsync(int taskId, DateTime? from, DateTime? to, string? status, int? assigneeId);
}


