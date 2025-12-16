using DocTask.Core.Dtos.Tasks;

namespace DocTask.Core.Interfaces.Repositories;

public interface IProgressRepository
{
    Task<List<ProgressDto>> GetProgressesByTaskAsync(int taskId);
    Task<Core.Models.Progress?> GetProgressByIdAsync(int progressId);
    Task<Core.Models.Progress> CreateProgressAsync(int taskId, UpdateProgressRequest request, int? updatedBy = null);
    Task<Core.Models.Progress?> UpdateProgressAsync(int progressId, UpdateProgressRequest request, int? updatedBy = null);
    Task<bool> DeleteProgressAsync(int progressId);
    Task<List<Core.Models.Progress>> GetProgressesForReviewAsync(int taskId, DateTime? from, DateTime? to, string? status, int? updatedBy);
}


