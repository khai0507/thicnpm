using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocTask.Core.Dtos.Gemini;
using static DocTask.Core.Dtos.Gemini.GeminiDto;

namespace DocTask.Core.Interfaces.Services
{
  public interface IGeminiService
  {
    Task<string> AskAsync(string userMessage);
    Task<string> GetFileContentAsync(string fileUrl);
    Task<ChatResponse> AskWithFileAsync(ChatRequest request, int fileId);
  }
}