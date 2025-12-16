using DocTask.Core.Dtos.Gemini;
using DocTask.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using static DocTask.Core.Dtos.Gemini.GeminiDto;

namespace DocTask.Api.Controllers
{
  [ApiController]
  [Route("api/v1/chat")]
  public class ChatGeminiController : ControllerBase
  {
    private readonly IGeminiService _geminiService;

    public ChatGeminiController(IGeminiService geminiService)
    {
      _geminiService = geminiService;
    }

    /// <summary>
    /// Chat với Gemini (không kèm file)
    /// </summary>
    [HttpPost("gemini")]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
      if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        return BadRequest(new { error = "UserMessage không được để trống." });

      try
      {
        var response = await _geminiService.AskAsync(request.UserMessage);
        return Ok(new { response });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    /// <summary>
    /// Chat với Gemini kèm file
    /// </summary>
    [HttpPost("gemini-with-file/{fileId}")]
    public async Task<IActionResult> PostWithFile([FromBody] ChatRequest request, int fileId)
    {
      if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        return BadRequest(new { error = "UserMessage không được để trống." });

      try
      {
        var response = await _geminiService.AskWithFileAsync(request, fileId);
        return Ok(response);
      }
      catch (ArgumentException ex)
      {
        return NotFound(new { error = ex.Message });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }
  }
}
