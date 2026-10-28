using System.Collections.Generic;

namespace LeaveFlow.Application.BaseResponse
{
    public class BaseResponse<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
