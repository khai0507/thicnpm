using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocTask.Core.Dtos.OpenAIDto;
using DocTask.Core.Dtos.Tasks;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using Tesseract;
using UglyToad.PdfPig;

namespace DocTask.Service.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly IUploadFileRepository _uploadFileRepository;
        private readonly OpenAI.OpenAIClient _client;
        private readonly HttpClient _httpClient;
        private readonly IProgressService _progressService;
        private readonly ITaskService _taskService;

        public OpenAIService(
            OpenAI.OpenAIClient client,
            IUploadFileRepository uploadFileRepository,
            IHttpClientFactory httpClientFactory,
            IProgressService progressService,
            ITaskService taskService)
        {
            _client = client;
            _uploadFileRepository = uploadFileRepository;
            _httpClient = httpClientFactory.CreateClient();
            _progressService = progressService;
            _taskService = taskService;
        }

        public async Task<OpenAIDto.ResponseDto> AskAsync(OpenAIDto.RequestDto request)
        {
            var chat = _client.GetChatClient("gpt-4o-mini");

            var message = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(@"
                    Bạn là một trợ lý AI thông minh, thân thiện và đa năng. 
                    Bạn có thể hỗ trợ nhiều chủ đề khác nhau như lập kế hoạch, giải thích kiến thức, tư vấn, hoặc trò chuyện thường ngày. 
                    Hãy trả lời chuyên nghiệp, dễ hiểu và phù hợp với ngữ cảnh."
                ),
                ChatMessage.CreateUserMessage(request.Prompt),
            };

            var response = await chat.CompleteChatAsync(message);
            var answer = response.Value.Content[0].Text;
            return new OpenAIDto.ResponseDto
            {
                Response = answer,
            };
        }

        public async Task<OpenAIDto.ResponseDto> AskWithFileAsync(OpenAIDto.RequestDto request, int fileId)
        {
            var file = await _uploadFileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                throw new ArgumentException("File not found");
            }

            var fileUrl = file.FilePath;
            var content = await GetFileContentAsync(fileUrl);

            var chat = _client.GetChatClient("gpt-4o-mini");

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(@"
                    Bạn là AI có thể đọc, phân tích nội dung file và trả lời câu hỏi."
                ),
                ChatMessage.CreateUserMessage($"Nội dung file:\n {content} \n\nCâu hỏi: {request}"),
            };

            var response = await chat.CompleteChatAsync(messages);
            var answer = response.Value.Content[0].Text;
            return new OpenAIDto.ResponseDto
            {
                Response = answer,
            };
        }

        public async Task<OpenAIDto.ResponseDto> AskSummaryReportAsync(
            OpenAIDto.RequestDto request, int taskId,
            DateTime? from,
            DateTime? to,
            string? status,
            int? assigneeId)
        {
            var reports = await _progressService.ReviewSubTaskProgressAsync(taskId, from, to, status, assigneeId);

            var contextData = JsonSerializer.Serialize(reports);

            var chat = _client.GetChatClient("gpt-4o-mini");

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(@"
                    Bạn là một trợ lý AI thông minh. 
                    Nhiệm vụ của bạn là hãy hỗ trợ người dùng trong việc tổng hợp báo cáo của người dùng cấp dưới sau khi họ đã hoàn thành nhiệm vụ được giao.
                    Báo cáo sẽ được nhập thông qua các đoạn văn bản do tôi gửi.
                    Khi người dùng cung cấp cho bạn một danh sách các báo cáo nhiệm vụ, hãy phân tích và tổng hợp chúng thành một báo cáo duy nhất, rõ ràng và súc tích.
                    Khi tổng hợp báo cáo, hãy đảm bảo rằng bạn bao gồm các điểm chính từ từng báo cáo, đồng thời loại bỏ bất kỳ thông tin thừa nào.
                    Khi hoàn thành, hãy trình bày báo cáo tổng hợp theo định dạng sau:
                    Tiêu đề: [Tiêu đề báo cáo tổng hợp]
                    Ngày: [Ngày hiện tại]
                    Người tổng hợp: [Tên người dùng]
                    Nội dung báo cáo:
                    [Nội dung báo cáo tổng hợp]
                    Những gì đạt được: 
                    [Danh sách các nhiệm vụ đã hoàn thành]
                    Những gì chưa đạt được:
                    [Danh sách các nhiệm vụ chưa hoàn thành]
                    Kế hoạch tiếp theo:
                    [Kế hoạch hành động tiếp theo]
                    Đề xuất:
                    [Bất kỳ đề xuất nào từ người dùng]
                    Góp ý(feedback):
                    [Bất kỳ góp ý nào từ người dùng]
                    Url file:
                    [Các tên filePath]
                    Hãy chắc chắn rằng báo cáo cuối cùng dễ đọc và hiểu, đồng thời phản ánh chính xác các nhiệm vụ đã được hoàn thành.
                    Hãy trả lời chuyên nghiệp, dễ hiểu và phù hợp với ngữ cảnh."
                ),
                ChatMessage.CreateUserMessage($"Đây là danh sách báo cáo:\n {contextData} \n\nCâu hỏi: {request}"),
            };

            var response = await chat.CompleteChatAsync(messages);
            var answer = response.Value.Content[0].Text;
            return new OpenAIDto.ResponseDto
            {
                Response = answer,
            };
        }

        public async Task<OpenAIDto.ResponseDto> AskSummaryReportFileAsync(
            OpenAIDto.RequestDto request, int taskId,
            DateTime? from,
            DateTime? to,
            string? status,
            int? assigneeId
        )
        {
            var reports = await _progressService.ReviewSubTaskProgressAsync(taskId, from, to, status, assigneeId);
            if (reports == null || !reports.Any())
            {
                return null;
            }

            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                foreach (var scheduled in report.ScheduledProgresses)
                {
                    foreach (var progress in scheduled.Progresses)
                    {
                        if (!string.IsNullOrEmpty(progress.FilePath))
                        {
                            try
                            {
                                var content = await GetFileContentAsync(progress.FilePath);

                                sb.AppendLine($"--- Report ID: {progress.ProgressId} ({progress.FileName}) ---");
                                sb.AppendLine(content);
                                sb.AppendLine();
                            }
                            catch (ArgumentException ex)
                            {
                                sb.AppendLine($"--- Report ID: {progress.ProgressId} ({progress.FileName}) ---");
                                sb.AppendLine($"[Error reading file: {ex.Message}]");
                                sb.AppendLine();
                            }
                        }
                    }
                }
            }

            var chat = _client.GetChatClient("gpt-4o-mini");

            var context = JsonSerializer.Serialize(reports); // dữ liệu báo cáo
            var allContent = sb.ToString(); // tất cả nội dung file

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(@"
                    Bạn là một trợ lý AI thông minh.
                    Nhiệm vụ của bạn là hãy hỗ trợ người dùng trong việc phân tích nội dung các file và tóm tắt lại thành một bản báo cáo tổng hợp chi tiết đồng thời trả lời câu hỏi của người dùng
                    Các file sẽ được tôi gửi.
                    Khi người dùng cung cấp cho bạn một nội dung trong file, hãy phân tích và tổng hợp chúng cùng với các file khác thành một bản báo cáo duy nhất, rõ ràng và súc tích.
                    Khi tổng hợp báo cáo, hãy đảm bảo rằng bạn bao gồm các điểm chính từ từng báo cáo"
                ),
                ChatMessage.CreateUserMessage($"Đây dũ liệu báo cáo \n\n là các file báo cáo:\n {allContent} \n\nCâu hỏi: {request}")
            };

            var response = await chat.CompleteChatAsync(messages);
            var answer = response.Value.Content[0].Text;
            return new OpenAIDto.ResponseDto
            {
                Response = answer
            };
        }

        public async Task<OpenAIDto.ListActionDto> AnalyzeFileAsync(OpenAIDto.RequestDto request)
        {
            var chat = _client.GetChatClient("gpt-4o-mini");

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(@"
                    Bạn là một trợ lý AI được thiết kế để phân tích và chuyển đổi yêu cầu thành các hành động có cấu trúc.
                    Khi nhận được một yêu cầu, hãy phân tích và trả về một danh sách các hành động cần thực hiện.
                    
                    Mỗi hành động cần có các thông tin:
                    - Action: create/update/delete
                    - EntityType: task/subtask/progress
                    - Payload: object chứa thông tin chi tiết của hành động
                        + Với task/subtask: title, description, assigneeId, createdAt
                        + Với progress: status, comment, percentageComplete
                    
                    Trả về kết quả dưới dạng mảng JSON của các hành động."
                ),
                ChatMessage.CreateUserMessage(request.Prompt)
            };

            var response = await chat.CompleteChatAsync(messages);
            var answer = response.Value.Content[0].Text;

            try
            {
                var actions = JsonSerializer.Deserialize<List<OpenAIDto.ActionDto>>(answer);
                return new OpenAIDto.ListActionDto
                {
                    ListAction = actions ?? new List<OpenAIDto.ActionDto>()
                };
            }
            catch (JsonException)
            {
                var defaultAction = new OpenAIDto.ActionDto
                {
                    Action = "create",
                    EntityType = "task",
                    Payload = new Dictionary<string, object>
                    {
                        {"title", request.Prompt},
                        {"description", "Created from natural language request"},
                        {"createdat", DateTime.UtcNow},
                    }
                };

                return new OpenAIDto.ListActionDto
                {
                    ListAction = new List<OpenAIDto.ActionDto>
                    {
                        defaultAction
                    }
                };
            }
        }

        // public async Task<object> ExecuteActionAsync(OpenAIDto.ActionDto action, int userId)
        // {
        //     switch (action.Action.ToLower())
        //     {
        //         case "create" when action.EntityType.ToLower() == "task":
        //             var dto = new TaskDto
        //             {
        //                 Title = action.Payload["title"].ToString() ?? "",
        //                 Description = action.Payload["description"].ToString() ?? "",
        //             };
        //             return await _taskService.CreateTaskAsync(dto, userId);

        //         default:
        //             throw new InvalidOperationException("Hành động không hợp lệ");
        //     }
        // }

        public async Task<string> GetFileContentAsync(string fileUrl)
        {
            var fileBytes = await _httpClient.GetByteArrayAsync(fileUrl);

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
                content = doc.MainDocumentPart.Document.Body.InnerText;
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
                        foreach (var cell in row.CellsUsed())
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