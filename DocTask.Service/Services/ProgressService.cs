using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Dtos.UploadFile;
using Microsoft.AspNetCore.Http;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Interfaces.Repositories;
using System.Linq;

namespace DocTask.Service.Services;

public class ProgressService : IProgressService
{
    private readonly IProgressRepository _progressRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITaskPermissionService _taskPermissionService;
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;

    public ProgressService(IProgressRepository progressRepository, IFileStorageService fileStorageService, ITaskPermissionService taskPermissionService, ITaskRepository taskRepository, IUserRepository userRepository)
    {
        _progressRepository = progressRepository;
        _fileStorageService = fileStorageService;
        _taskPermissionService = taskPermissionService;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
    }

    public async Task<UpdateProgressResponse> UpdateProgressAsync(int taskId, UpdateProgressRequest request, int? updatedBy = null)
    {
        // If file stream present, upload to cloud and set ReportFilePath
        if (request.ReportFileStream != null && !string.IsNullOrWhiteSpace(request.ReportFileName) && request.SubmittedByUserId > 0)
        {
            var formFile = new FormFile(request.ReportFileStream, 0, request.ReportFileStream.Length, "file", request.ReportFileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
            var uploadDto = await _fileStorageService.UploadFileAsync(new UploadFileRequest { File = formFile }, request.SubmittedByUserId);
            request.ReportFilePath = uploadDto.FilePath;
        }

        var progress = await _progressRepository.CreateProgressAsync(taskId, request, updatedBy);

        return new UpdateProgressResponse
        {
            ProgressId = progress.ProgressId,
            TaskId = progress.TaskId,
            Proposal = progress.Proposal,
            Result = progress.Result,
            Feedback = progress.Feedback,
            Status = progress.Status,
            FileName = progress.FileName,
            FilePath = progress.FilePath,
            UpdatedAt = progress.UpdatedAt,
            UpdatedBy = progress.UpdatedBy
        };
    }

    public Task<List<ProgressDto>> GetProgressesByTaskAsync(int taskId)
        => _progressRepository.GetProgressesByTaskAsync(taskId);

    public Task<Core.Models.Progress?> GetProgressByIdAsync(int progressId)
        => _progressRepository.GetProgressByIdAsync(progressId);

    public async Task<Core.Models.Progress?> UpdateProgressEntryAsync(int progressId, UpdateProgressRequest request, int? updatedBy = null)
    {
        // If file stream present, upload to cloud and set ReportFilePath
        if (request.ReportFileStream != null && !string.IsNullOrWhiteSpace(request.ReportFileName) && request.SubmittedByUserId > 0)
        {
            var formFile = new FormFile(request.ReportFileStream, 0, request.ReportFileStream.Length, "file", request.ReportFileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
            var uploadDto = await _fileStorageService.UploadFileAsync(new UploadFileRequest { File = formFile }, request.SubmittedByUserId);
            request.ReportFilePath = uploadDto.FilePath;
        }

        return await _progressRepository.UpdateProgressAsync(progressId, request, updatedBy);
    }

    public async Task<bool> DeleteProgressAsync(int progressId)
    {
        // Lấy progress để kiểm tra file path trước khi xóa
        var progress = await _progressRepository.GetProgressByIdAsync(progressId);
        if (progress == null)
            return false;

        // Xóa file nếu có file path (sử dụng service có sẵn)
        if (!string.IsNullOrEmpty(progress.FilePath))
        {
            // Tìm file trong database để lấy fileId
            var files = await _fileStorageService.GetFileByUserIdAsync(progress.UpdatedBy ?? 0);
            var fileToDelete = files?.FirstOrDefault(f => f.FilePath == progress.FilePath);
            if (fileToDelete != null)
            {
                await _fileStorageService.DeleteFileAsync(fileToDelete.FileId, progress.UpdatedBy ?? 0);
            }
        }

        return await _progressRepository.DeleteProgressAsync(progressId);
    }

    public async Task<List<ProgressReviewByUserDto>> ReviewProgressByUserAsync(int taskId, DateTime? from, DateTime? to, string? status)
    {
        var records = await _progressRepository.GetProgressesForReviewAsync(taskId, from, to, status, null);
        // Lấy thông tin task chuẩn theo taskId (kể cả khi chưa có report)
        var taskModel = await _taskRepository.GetTaskByIdAsync(taskId);
        if (taskModel == null) return new List<ProgressReviewByUserDto>();
        var frequencyType = taskModel.Frequency?.FrequencyType?.Trim().ToLower() ?? "daily";

        // Nhóm theo user (chỉ các user có report)
        var userGroups = records
            .Where(p => p.UpdatedBy.HasValue)
            .GroupBy(p => p.UpdatedBy!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Lấy tất cả user được phân công (bao gồm cả người chưa có report)
        var authorizedUserIds = await _taskPermissionService.GetAuthorizedUserIdsAsync(taskId);

        var result = new List<ProgressReviewByUserDto>();
        var userNameCache = new Dictionary<int, string>();

        foreach (var userId in authorizedUserIds)
        {
            userGroups.TryGetValue(userId, out var userReports);
            var user = userReports?.FirstOrDefault()?.UpdatedByNavigation;
            string userFullName;
            if (user != null && !string.IsNullOrWhiteSpace(user.FullName))
            {
                userFullName = user.FullName;
            }
            else if (userNameCache.TryGetValue(userId, out var cachedName))
            {
                userFullName = cachedName;
            }
            else
            {
                var userEntity = await _userRepository.GetByIdAsync(userId);
                userFullName = userEntity?.FullName ?? string.Empty;
                userNameCache[userId] = userFullName;
            }
            
            var userDto = new ProgressReviewByUserDto
            {
                UpdatedByFullName = userFullName,
                Periods = new Dictionary<string, ProgressReviewPeriodDto>()
            };

            if (userReports != null && userReports.Count > 0)
            {
                // Nhóm theo period (ngày/tuần/tháng)
                var periodGroups = userReports
                    .GroupBy(p => GetPeriodDate(p.UpdatedAt, frequencyType))
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var periodGroup in periodGroups)
                {
                    var periodDate = periodGroup.Key;
                    var periodKey = GetPeriodKey(periodDate, frequencyType);
                    
                    // Lấy báo cáo mới nhất trong kỳ
                    var latestReport = periodGroup.OrderByDescending(p => p.UpdatedAt).First();

                    var periodDto = new ProgressReviewPeriodDto
                    {
                        Status = latestReport.Status ?? string.Empty,
                        FilePath = latestReport.FilePath ?? string.Empty,
                        Proposal = latestReport.Proposal ?? string.Empty,
                        Result = latestReport.Result ?? string.Empty,
                        Feedback = latestReport.Feedback ?? string.Empty
                    };

                    userDto.Periods[periodKey] = periodDto;
                }
            }

            result.Add(userDto);
        }

        return result;
    }

    public async Task<List<SubTaskProgressReviewDto>> ReviewSubTaskProgressAsync(int taskId, DateTime? from, DateTime? to, string? status, int? assigneeId)
    {
        Console.WriteLine($"[REVIEW-DEBUG] ReviewSubTaskProgressAsync called for task {taskId}");
        
        var records = await _progressRepository.GetProgressesForReviewAsync(taskId, from, to, status, assigneeId);
        // Lấy thông tin task chuẩn theo taskId (kể cả khi chưa có report)
        var taskModel = await _taskRepository.GetTaskByIdAsync(taskId);
        if (taskModel == null) 
        {
            Console.WriteLine($"[REVIEW-DEBUG] Task {taskId} not found");
            return new List<SubTaskProgressReviewDto>();
        }
        
        var frequencyType = taskModel.Frequency?.FrequencyType?.ToLower() ?? "daily";
        var intervalValue = taskModel.Frequency?.IntervalValue ?? 1;
        
        Console.WriteLine($"[REVIEW-DEBUG] Task {taskId}: frequencyType='{frequencyType}', intervalValue={intervalValue}");
        Console.WriteLine($"[REVIEW-DEBUG] Task {taskId}: frequencyId={taskModel.FrequencyId}, frequency object={taskModel.Frequency?.FrequencyType}");
        var weeklyDaysRaw = taskModel.Frequency?.FrequencyDetails
            ?.Where(d => d.DayOfWeek.HasValue)
            .Select(d => d.DayOfWeek!.Value)
            .ToList() ?? new List<int>();
        var startDate = (taskModel.StartDate ?? DateTime.UtcNow).Date;
        var dueDate = (taskModel.DueDate ?? startDate).Date;
        if (startDate > dueDate)
            return new List<SubTaskProgressReviewDto>();

        // Tạo danh sách các kỳ theo lịch trình
        // Áp dụng khoảng filter from/to nếu có
        var effectiveStart = from.HasValue ? (from.Value.Date > startDate ? from.Value.Date : startDate) : startDate;
        var effectiveEnd = to.HasValue ? (to.Value.Date < dueDate ? to.Value.Date : dueDate) : dueDate;
        if (effectiveStart > effectiveEnd)
            return new List<SubTaskProgressReviewDto>();

        var scheduledPeriods = GenerateScheduledPeriods(effectiveStart, effectiveEnd, frequencyType, intervalValue, weeklyDaysRaw);

        // Nhóm theo user (chỉ các user có report)
        var userGroups = records
            .Where(p => p.UpdatedBy.HasValue)
            .GroupBy(p => p.UpdatedBy!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Lấy tất cả user được phân công (bao gồm cả người chưa có report)
        var authorizedUserIds = await _taskPermissionService.GetAuthorizedUserIdsAsync(taskId);
        if (assigneeId.HasValue)
        {
            authorizedUserIds = authorizedUserIds.Where(id => id == assigneeId.Value).ToList();
        }

        var result = new List<SubTaskProgressReviewDto>();
        var userNameCache2 = new Dictionary<int, string>();

        foreach (var userId in authorizedUserIds)
        {
            userGroups.TryGetValue(userId, out var userReports);
            var user = userReports?.FirstOrDefault()?.UpdatedByNavigation;
            string userFullName;
            if (user != null && !string.IsNullOrWhiteSpace(user.FullName))
            {
                userFullName = user.FullName;
            }
            else if (userNameCache2.TryGetValue(userId, out var cachedName))
            {
                userFullName = cachedName;
            }
            else
            {
                var userEntity = await _userRepository.GetByIdAsync(userId);
                userFullName = userEntity?.FullName ?? string.Empty;
                userNameCache2[userId] = userFullName;
            }
            
            var userDto = new SubTaskProgressReviewDto
            {
                UserId = userId,
                UserName = userFullName,
                ScheduledProgresses = new List<ScheduledProgressDto>()
            };

            // Tạo scheduled progresses cho user này
            for (int i = 0; i < scheduledPeriods.Count; i++)
            {
                var period = scheduledPeriods[i];
                var periodIndex = i + 1; // Sequential index per generated period
                
                // Tìm progress trong kỳ này
                var periodProgresses = (userReports ?? new List<Core.Models.Progress>())
                    .Where(p => IsInPeriod(p.UpdatedAt, period.StartDate, period.EndDate, frequencyType))
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToList();

                var scheduledProgress = new ScheduledProgressDto
                {
                    PeriodIndex = periodIndex,
                    PeriodStartDate = period.StartDate,
                    PeriodEndDate = period.EndDate,
                    // Nếu có báo cáo: lấy status của báo cáo mới nhất; nếu không: pending
                    Status = periodProgresses.Any() ? (periodProgresses.First().Status ?? "in_progress") : "pending",
                    Date = periodProgresses.FirstOrDefault()?.UpdatedAt ?? DateTime.MinValue,
                    Progresses = new List<ProgressDetailDto>()
                };

                if (periodProgresses.Any())
                {
                    // Chỉ lấy báo cáo mới nhất trong kỳ
                    var latest = periodProgresses.First();
                    scheduledProgress.Progresses.Add(new ProgressDetailDto
                    {
                        ProgressId = latest.ProgressId,
                        Status = latest.Status ?? "",
                        UpdatedBy = latest.UpdatedBy ?? 0,
                        UpdateByName = latest.UpdatedByNavigation?.FullName,
                        Proposal = latest.Proposal,
                        Result = latest.Result,
                        Feedback = latest.Feedback,
                        UpdatedAt = latest.UpdatedAt,
                        FileName = latest.FileName,
                        FilePath = latest.FilePath ?? ""
                    });
                }
                else
                {
                    // Không có progress, tạo placeholder
                    scheduledProgress.Progresses.Add(new ProgressDetailDto
                    {
                        ProgressId = 0,
                        Status = "Chưa có báo cáo cho mốc này",
                        UpdatedBy = 0,
                        UpdateByName = null,
                        Proposal = null,
                        Result = null,
                        Feedback = null,
                        UpdatedAt = null,
                        FileName = null,
                        FilePath = ""
                    });
                }

                userDto.ScheduledProgresses.Add(scheduledProgress);
            }

            result.Add(userDto);
        }

        return result;
    }

    private List<(DateTime StartDate, DateTime EndDate)> GenerateScheduledPeriods(DateTime startDate, DateTime dueDate, string frequencyType, int intervalValue, List<int>? weeklyDaysRaw = null)
    {
        var periods = new List<(DateTime StartDate, DateTime EndDate)>();
        var current = startDate.Date;
        var endBoundary = dueDate.Date;

        if (intervalValue <= 0) intervalValue = 1;

        if (frequencyType == "weekly")
        {
            Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] Weekly periods: start={startDate:yyyy-MM-dd}, end={endBoundary:yyyy-MM-dd}, interval={intervalValue}");
            Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] Weekly days from DB: {string.Join(",", weeklyDaysRaw ?? new List<int>())}");
            
            // Sử dụng day từ database (1=Chủ nhật, 2=Thứ 2, ..., 7=Thứ 7)
            var targetDays = NormalizeWeeklyDays(weeklyDaysRaw ?? new List<int>()).ToList();
            Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] Normalized days: {string.Join(",", targetDays)}");
            
            if (targetDays.Count == 0)
            {
                Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] No valid days specified, using Sunday as default");
                targetDays.Add(DayOfWeek.Sunday);
            }
            
            // Tìm ngày báo cáo đầu tiên từ startDate
            var firstReportDay = FindNextReportDay(startDate, targetDays);
            var firstStart = startDate;
            var firstEnd = firstReportDay <= endBoundary ? firstReportDay : endBoundary;
            periods.Add((firstStart, firstEnd));
            Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] Period 1: {firstStart:yyyy-MM-dd} to {firstEnd:yyyy-MM-dd} (first report period)");

            // Các kỳ tiếp theo: mỗi kỳ cách nhau intervalValue tuần, kết thúc vào ngày báo cáo
            var spanDays = 7 * intervalValue;
            var lastEnd = firstReportDay;
            int periodCount = 1;
            while (true)
            {
                var nextEnd = lastEnd.AddDays(spanDays);
                var nextStart = lastEnd.AddDays(1); // kỳ tiếp theo bắt đầu từ ngày sau kỳ trước
                
                // Chỉ tạo kỳ mới nếu có đủ thời gian cho ít nhất 1 ngày báo cáo
                if (nextStart > endBoundary) break;
                
                // Tìm ngày báo cáo gần nhất trong kỳ này
                var reportDay = FindNextReportDay(nextStart, targetDays);
                if (reportDay > endBoundary)
                {
                    Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] Skipping period {periodCount + 1}: no report day in final period");
                    break;
                }
                
                var endDate = reportDay;
                
                // Chỉ thêm kỳ nếu có ít nhất 1 ngày (tránh kỳ rỗng)
                if (nextStart <= endDate)
                {
                    periods.Add((nextStart, endDate));
                    periodCount++;
                    Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] Period {periodCount}: {nextStart:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                }
                lastEnd = nextEnd;
            }
            Console.WriteLine($"[PROGRESS-SERVICE-DEBUG] Total periods generated: {periods.Count}");
        }
        else
        {
            while (current <= endBoundary)
            {
                var endDate = frequencyType switch
                {
                    "daily" => current.AddDays(intervalValue - 1),
                    "monthly" => current.AddMonths(intervalValue).AddDays(-1),
                    _ => current.AddDays(intervalValue - 1)
                };

                if (endDate > endBoundary) endDate = endBoundary;

                periods.Add((current, endDate));

                current = frequencyType switch
                {
                    "daily" => current.AddDays(intervalValue),
                    "monthly" => current.AddMonths(intervalValue),
                    _ => current.AddDays(intervalValue)
                };
            }
        }

        return periods;
    }

    // Chuẩn hóa danh sách ngày tuần theo quy ước người dùng: 1=Chủ nhật, 2=Thứ 2,... -> .NET: Sunday=0..Saturday=6
    private List<DayOfWeek> NormalizeWeeklyDays(List<int>? days)
    {
        var result = new List<DayOfWeek>();
        if (days == null) return result;
        foreach (var d in days)
        {
            // Quy ước: 1=Chủ nhật, 2=Thứ 2, ..., 7=Thứ 7
            // .NET: 0=Chủ nhật, 1=Thứ 2, ..., 6=Thứ 7
            // Mapping: 1->0, 2->1, 3->2, 4->3, 5->4, 6->5, 7->6
            if (d >= 1 && d <= 7)
            {
                var dotnetDay = (DayOfWeek)((d - 1) % 7);
                result.Add(dotnetDay);
            }
        }
        return result;
    }

    

    private DateTime FindNextReportDay(DateTime startDate, List<DayOfWeek> targetDays)
    {
        // Tìm ngày báo cáo gần nhất từ startDate trong danh sách targetDays
        var current = startDate.Date;
        
        // Kiểm tra trong 7 ngày tới
        for (int i = 0; i < 7; i++)
        {
            if (targetDays.Contains(current.DayOfWeek))
            {
                Console.WriteLine($"[FIND-REPORT-DAY] Found report day: {current:yyyy-MM-dd} ({current.DayOfWeek})");
                return current;
            }
            current = current.AddDays(1);
        }
        
        // Nếu không tìm thấy, trả về ngày cuối cùng
        Console.WriteLine($"[FIND-REPORT-DAY] No report day found, using last day: {current.AddDays(-1):yyyy-MM-dd}");
        return current.AddDays(-1);
    }

    private bool IsInPeriod(DateTime date, DateTime periodStart, DateTime periodEnd, string frequencyType)
    {
        return frequencyType switch
        {
            "daily" => date.Date >= periodStart.Date && date.Date <= periodEnd.Date,
            "weekly" => date >= periodStart && date <= periodEnd,
            "monthly" => date >= periodStart && date <= periodEnd,
            _ => date.Date >= periodStart.Date && date.Date <= periodEnd.Date
        };
    }

    private DateTime GetPeriodDate(DateTime date, string frequencyType)
    {
        return frequencyType switch
        {
            "daily" => date.Date,
            "weekly" => GetWeekStart(date),
            "monthly" => new DateTime(date.Year, date.Month, 1),
            _ => date.Date
        };
    }

    private string GetPeriodKey(DateTime periodDate, string frequencyType)
    {
        return frequencyType switch
        {
            "daily" => periodDate.ToString("yyyy-MM-dd"),
            "weekly" => $"Week {GetWeekOfYear(periodDate)} - {periodDate.Year}",
            "monthly" => periodDate.ToString("yyyy-MM"),
            _ => periodDate.ToString("yyyy-MM-dd")
        };
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var monday = date.AddDays(-dayOfWeek + 1);
        return monday.Date;
    }

    private int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }
}


