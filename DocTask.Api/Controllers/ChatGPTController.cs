using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DocTask.Core.Dtos.OpenAIDto;
using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocTask.Api.Controllers
{
    [ApiController]
    [Route("api/v1/chat")]
    public class ChatGPTController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        public ChatGPTController(IOpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        [HttpPost("GPT")]
        public async Task<IActionResult> Ask([FromBody] string request)
        {
            var response = await _openAIService.AskAsync(new OpenAIDto.RequestDto { Prompt = request });
            return Content(response.Response, "text/plain; charset=utf-8");
        }

        [HttpPost("GPT/{fileId}")]
        public async Task<IActionResult> AskWithFile([FromBody] string request, int fileId)
        {
            var response = await _openAIService.AskWithFileAsync(new OpenAIDto.RequestDto { Prompt = request }, fileId);
            return Content(response.Response, "text/plain; charset=utf-8");
        }

        [HttpPost("GPT/task/{taskId}")]
        public async Task<IActionResult> AskSummaryReport(
            int taskId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? status,
            [FromQuery] int? assigneeId,
            [FromBody] string request)
        {
            var response = await _openAIService.AskSummaryReportAsync(new OpenAIDto.RequestDto { Prompt = request }, taskId, from, to, status, assigneeId);
            return Content(response.Response, "text/plain; charset=utf-8");
        }

        [HttpPost("GPT/file/{taskId}")]
        public async Task<IActionResult> AskSummaryReportFile(
            int taskId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? status,
            [FromQuery] int? assigneeId,
            [FromBody] string request)
        {
            var response = await _openAIService.AskSummaryReportFileAsync(new OpenAIDto.RequestDto { Prompt = request }, taskId, from, to, status, assigneeId);
            return Content(response.Response, "text/plain; charset=utf-8");
        }

        [HttpPost("GPT/analyze")]
        public async Task<IActionResult> Analyze([FromBody] string request)
        {
            var suggestedAction = await _openAIService.AnalyzeFileAsync(new OpenAIDto.RequestDto { Prompt = request });
            return Ok(new
            {
                message = "AI đã phân tích xong, vui lòng xác nhận trước khi thực hiện.",
                suggestedAction
            });
        }

        // [HttpPost("GPT/execute")]
        // public async Task<IActionResult> Execute([FromBody] OpenAIDto.ActionDto action)
        // {
        //     var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
        //     int userId = int.Parse(userIdClaim);

        //     var result = await _openAIService.ExecuteActionAsync(action, userId);
        //     return Ok(new
        //     {
        //         message = $"Action {action.Action} trên {action.EntityType} đã được thực hiện",
        //         result,                
        //     });
        // }
    }
}