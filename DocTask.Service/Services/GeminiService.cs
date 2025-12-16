using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocTask.Core.Dtos.Gemini;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Tesseract;
using UglyToad.PdfPig;
using static DocTask.Core.Dtos.Gemini.GeminiDto;

namespace DocTask.Service.Services
{
  public class GeminiService : IGeminiService
  {
    private readonly HttpClient _httpClient;
    private readonly string _geminiApiKey;
    private readonly IUploadFileRepository _uploadFileRepository;

    public GeminiService(HttpClient httpClient, GeminiDto.GeminiOptions options, IUploadFileRepository uploadFileRepository)
    {
      _httpClient = httpClient;
      _geminiApiKey = options.ApiKey;
      _uploadFileRepository = uploadFileRepository;
    }

    public async Task<string> AskAsync(string userMessage)
    {
      var systemPrompt = GeminiDto.GeminiPrompts.PlanningAssistant;

      var requestBody = new
      {
        contents = new object[]
          {
            new
            {
                role = "user",
                parts = new object[]
                {
                    new { text = systemPrompt }
                }
            },
            new
            {
                role = "user",
                parts = new object[]
                {
                    new { text = userMessage }
                }
            }
          }
      };

      var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_geminiApiKey}";

      HttpResponseMessage response;

      try
      {
        response = await _httpClient.PostAsJsonAsync(url, requestBody);

        // Log status code
        Console.WriteLine($"Status Code: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();

        // Log body để xem chi tiết lý do 403
        Console.WriteLine("Response Body:");
        Console.WriteLine(content);

        response.EnsureSuccessStatusCode(); // sẽ ném exception nếu != 2xx

        using var doc = JsonDocument.Parse(content);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "No response text";
      }
      catch (HttpRequestException ex)
      {
        Console.WriteLine("HTTP Request Exception:");
        Console.WriteLine(ex.Message);
        throw; // có thể throw lại hoặc return thông báo custom
      }
    }


    public async Task<ChatResponse> AskWithFileAsync(ChatRequest request, int fileId)
    {
      var file = await _uploadFileRepository.GetByIdAsync(fileId);
      if (file == null)
        throw new ArgumentException("File not found");

      var fileUrl = file.FilePath;
      var fileContent = await GetFileContentAsync(fileUrl);

      // Gộp nội dung file với tin nhắn người dùng
      var combinedMessage = $"[FILE CONTENT]\n{fileContent}\n\n[USER MESSAGE]\n{request.UserMessage}";

      var responseText = await AskAsync(combinedMessage);

      return new ChatResponse
      {
        Response = responseText
      };
    }

    public async Task<string> GetFileContentAsync(string fileUrl)
    {
      if (string.IsNullOrWhiteSpace(fileUrl))
        throw new ArgumentException("File URL is null or empty", nameof(fileUrl));

      // Nếu fileUrl không phải URL tuyệt đối, ghép BaseAddress của MinIO
      if (!Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
      {
        string baseAddress = "https://minio-production-f30c.up.railway.app/doctask/";
        fileUrl = $"{baseAddress}{Uri.EscapeDataString(fileUrl)}";
      }

      // Chuyển fileUrl thành URI hợp lệ
      Uri fileUri;
      try
      {
        var uri = new Uri(fileUrl);

        // Encode các ký tự không hợp lệ trong path
        var encodedPath = string.Join(
          "/",
          uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(segment => Uri.EscapeDataString(segment))
        );

        encodedPath = "/" + encodedPath; // thêm / đầu tiên nếu cần


        // Bao gồm query nếu có
        var encodedUriString = $"{uri.Scheme}://{uri.Host}{encodedPath}{uri.Query}";
        fileUri = new Uri(encodedUriString);
      }
      catch (UriFormatException ex)
      {
        throw new InvalidOperationException($"Invalid file URL: {fileUrl}", ex);
      }

      // Lấy file bytes
      var fileBytes = await _httpClient.GetByteArrayAsync(fileUri);

      var extension = Path.GetExtension(fileUrl).ToLowerInvariant();
      string content = "";

      if (extension == ".txt")
      {
        content = Encoding.UTF8.GetString(fileBytes);
      }

      else if (extension == ".pdf")
      {
        using var pdf = PdfDocument.Open(fileBytes);
        var sb = new StringBuilder();

        foreach (var page in pdf.GetPages())
        {
          sb.AppendLine(page.Text);
        }

        content = sb.ToString();
      }

      else if (extension == ".docx" || extension == ".doc")
      {
        using var stream = new MemoryStream(fileBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        content = doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
      }

      else if (extension == ".xls" || extension == ".xlsx")
      {
        using var stream = new MemoryStream(fileBytes);
        using var xls = new ClosedXML.Excel.XLWorkbook(stream);
        var sb = new StringBuilder();

        foreach (var worksheet in xls.Worksheets)
        {
          sb.AppendLine($"--- Sheet: {worksheet.Name} ---");

          foreach (var row in worksheet.RowsUsed())
          {
            foreach (var cell in worksheet.CellsUsed())
            {
              sb.Append(cell.Value.ToString());
              sb.Append("\t");
            }

            sb.AppendLine();
          }

          sb.AppendLine();
        }

        content = sb.ToString();
      }

      else if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif")
      {
        var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

        if (!Directory.Exists(dataPath))
          throw new DirectoryNotFoundException($"Te$$Data not found: {dataPath}");

        using var engine = new TesseractEngine(dataPath, "eng+vie", EngineMode.Default);
        using var img = Pix.LoadFromMemory(fileBytes);
        using var page = engine.Process(img);
        content = page.GetText();
      }

      else
      {
        throw new NotSupportedException("File format not supported for reading");
      }

      return content;
    }
  }
}