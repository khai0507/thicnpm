using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocTask.Core.Dtos.OpenAIDto
{
    public class OpenAIDto
    {
        public class RequestDto
        {
            public string Prompt { get; set; } = "";
        }

        public class ResponseDto
        {
            public string Response { get; set; } = "";
        }

        public class ActionDto
        {
            public string Action { get; set; } = "";
            public string EntityType { get; set; } = "";
            public int? TargetId { get; set; }
            public Dictionary<string, object> Payload { get; set; } = new();
        }

        public class ListActionDto
        {
            public List<ActionDto> ListAction { get; set; } = new();
        }
    }
}