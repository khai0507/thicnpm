using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocTask.Data.Repositories;

public class ProgressRepository : IProgressRepository
{
    private readonly ApplicationDbContext _context;

    public ProgressRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProgressDto>> GetProgressesByTaskAsync(int taskId)
    {
        return await _context.Progresses
            .Where(p => p.TaskId == taskId)
            .Include(p => p.UpdatedByNavigation)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new ProgressDto
            {
                ProgressId = p.ProgressId,
                TaskId = p.TaskId,
                PeriodId = p.PeriodId,
                PercentageComplete = p.PercentageComplete,
                Comment = p.Comment,
                Status = p.Status,
                UpdatedBy = p.UpdatedBy,
                UpdatedAt = p.UpdatedAt,
                FileName = p.FileName,
                FilePath = p.FilePath,
                Proposal = p.Proposal,
                Result = p.Result,
                Feedback = p.Feedback,
                UpdatedByUserName = p.UpdatedByNavigation != null ? p.UpdatedByNavigation.Username : null,
                UpdatedByFullName = p.UpdatedByNavigation != null ? p.UpdatedByNavigation.FullName : null
            })
            .ToListAsync();
    }

    public async Task<Progress?> GetProgressByIdAsync(int progressId)
    {
        return await _context.Progresses
            .Include(p => p.UpdatedByNavigation)
            .FirstOrDefaultAsync(p => p.ProgressId == progressId);
    }

    public async Task<Progress> CreateProgressAsync(int taskId, UpdateProgressRequest request, int? updatedBy = null)
    {
        var progress = new Progress
        {
            TaskId = taskId,
            Proposal = request.Proposal,
            Result = request.Result,
            Feedback = request.Feedback,
            Status = request.Status,
            FileName = request.ReportFileName,
            FilePath = request.ReportFilePath,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };

        _context.Progresses.Add(progress);
        await _context.SaveChangesAsync();
        return progress;
    }

    public async Task<Progress?> UpdateProgressAsync(int progressId, UpdateProgressRequest request, int? updatedBy = null)
    {
        var existing = await _context.Progresses.FirstOrDefaultAsync(p => p.ProgressId == progressId);
        if (existing == null) return null;

        existing.Proposal = request.Proposal;
        existing.Result = request.Result;
        existing.Feedback = request.Feedback;
        existing.Status = request.Status;
        existing.FileName = request.ReportFileName ?? existing.FileName;
        existing.FilePath = request.ReportFilePath ?? existing.FilePath;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteProgressAsync(int progressId)
    {
        var existing = await _context.Progresses.FirstOrDefaultAsync(p => p.ProgressId == progressId);
        if (existing == null) return false;
        _context.Progresses.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Progress>> GetProgressesForReviewAsync(int taskId, DateTime? from, DateTime? to, string? status, int? updatedBy)
    {
        var query = _context.Progresses
            .Include(p => p.UpdatedByNavigation)
            .AsQueryable();

        query = query.Where(p => p.TaskId == taskId);

        if (from.HasValue)
        {
            query = query.Where(p => p.UpdatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.UpdatedAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status != null && p.Status == status);
        }

        if (updatedBy.HasValue)
        {
            query = query.Where(p => p.UpdatedBy == updatedBy);
        }

        return await query
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }
}


