using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocTask.Core.Dtos.OpenAIDto;

namespace DocTask.Core.Interfaces.Services
{
    public interface IOpenAIService
    {
        Task<OpenAIDto.ResponseDto> AskAsync(OpenAIDto.RequestDto request);
        Task<string> GetFileContentAsync(string fileUrl);
        Task<OpenAIDto.ResponseDto> AskWithFileAsync(OpenAIDto.RequestDto request, int fileId);
        Task<OpenAIDto.ResponseDto> AskSummaryReportAsync(OpenAIDto.RequestDto request, int taskId, DateTime? from, DateTime? to, string? status, int? assigneeId);
        Task<OpenAIDto.ResponseDto> AskSummaryReportFileAsync(OpenAIDto.RequestDto request, int taskId, DateTime? from, DateTime? to, string? status, int? assigneeId);
        Task<OpenAIDto.ListActionDto> AnalyzeFileAsync(OpenAIDto.RequestDto request);
        // Task<object> ExecuteActionAsync(OpenAIDto.ActionDto action, int userId);
    }
}