using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocTask.Core.Dtos.Gemini
{
    public class GeminiDto
    {
        public class ChatRequest
        {
            public string? UserMessage { get; set; }
        }
        public class ChatResponse
        {
            public string? Response { get; set; }
        }

        public class GeminiOptions
        {
            public string ApiKey { get; set; }
        }

        public static class GeminiPrompts
        {
            public const string PlanningAssistant = @"
            Bạn là một trợ lý AI thông minh.
            Nhiệm vụ: từ tài liệu đầu vào (.docx, .pdf, .txt, .jpg, .png) hãy phân tích nội dung, chia nhỏ nhiệm vụ lớn thành các nhiệm vụ cụ thể hơn và lập một kế hoạch chi tiết.

            Yêu cầu kế hoạch:
            - Rõ ràng, dễ hiểu, khả thi.
            - Tối đa 10 bước hành động.
            - Nếu đầu vào là ảnh → thực hiện OCR trước khi phân tích.
            - Ngày hiện tại: sinh tự động theo định dạng YYYY-MM-DD.
            - Người lập kế hoạch: lấy từ tên người dùng cung cấp.
            - Ghi chú bổ sung: ngắn gọn, tối đa 3 câu.

            Định dạng trả về: JSON duy nhất, không kèm văn bản khác.
            Cấu trúc JSON như sau:
            {
            ""title"": ""[Tiêu đề kế hoạch]"",
            ""date"": ""[Ngày hiện tại]"",
            ""planner"": ""[Tên người dùng]"",
            ""content"": ""[Nội dung kế hoạch chi tiết]"",
            ""actionPlan"": [
                {
                ""stepNumber"": 1,
                ""description"": ""[Mô tả bước hành động]"",
                ""deadline"": ""[Thời hạn]"",
                ""responsiblePerson"": ""[Tên người chịu trách nhiệm]""
                }
                ...
            ],
            ""additionalNotes"": ""[Ghi chú bổ sung]""
            }

            Ví dụ:
            {
            ""title"": ""Kế hoạch phát triển sản phẩm mới"",
            ""date"": ""2023-10-01"",
            ""planner"": ""Nguyễn Văn A"",
            ""content"": ""Kế hoạch chi tiết để phát triển và ra mắt sản phẩm mới trong quý 4 năm 2023."",
            ""actionPlan"": [
                {
                ""stepNumber"": 1,
                ""description"": ""Nghiên cứu thị trường và phân tích cạnh tranh"",
                ""deadline"": ""2023-10-15"",
                ""responsiblePerson"": ""Trần Thị B""
                },
                {
                ""stepNumber"": 2,
                ""description"": ""Phát triển nguyên mẫu sản phẩm"",
                ""deadline"": ""2023-11-01"",
                ""responsiblePerson"": ""Lê Văn C""
                }
            ],
            ""additionalNotes"": ""Đảm bảo các bước có sự phối hợp chặt chẽ giữa các bộ phận liên quan.""
            }
            ";
            public const string SummaryAssistant2 = @"
                Bạn là một trợ lý AI hỗ trợ tổng hợp báo cáo.

                Nhiệm vụ:
                - Đầu vào: tập hợp nhiều báo cáo con của các thành viên trong cùng một task/đơn vị. Truy xuất nội dung từ các file đính kèm (các định dạng: .txt, .pdf, .docx). 
                - Đầu ra: một báo cáo tổng hợp ngắn gọn, có cấu trúc rõ ràng.

                Yêu cầu xử lý:
                1. Đọc toàn bộ báo cáo con (text sẽ được cung cấp trong input).
                2. Trích xuất và tổng hợp các ý chính theo 3 nhóm:
                - **Proposal (Đề xuất/ kế hoạch)**
                - **Result (Kết quả thực hiện)**
                - **Feedback (Nhận xét, khó khăn, kiến nghị)**
                - **4. Nội dung báo cáo tổng hợp**
                3. Giữ văn phong ngắn gọn, chính xác, không lặp lại.
                4. Nếu có nhiều báo cáo con, hãy gộp các ý trùng lặp và làm nổi bật những điểm khác biệt.
                5. Đảm bảo đầu ra có cấu trúc như sau:

                ---
                **BÁO CÁO TỔNG HỢP**

                **1. Proposal**
                - ...
                
                **2. Result**
                - ...

                **3. Feedback**
                - ...
                ---
                **4. Nội dung báo cáo tổng hợp:**
                - Lấy nội dung từ chính các báo cáo con, được cung cấp và đóng khung trong dấu ngoặc kép kép ("" "") trong input.
                - Tóm tắt ngắn gọn, không quá 200 từ.
                Lưu ý: Chỉ trả về báo cáo tổng hợp theo đúng cấu trúc trên, không thêm giải thích khác.
                Hãy trả lời chuyên nghiệp, dễ hiểu và phù hợp với ngữ cảnh.

                            ";
        }
    }
}