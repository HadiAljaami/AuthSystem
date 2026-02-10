namespace AuthSystem.Api.Application.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public ApiError? Error { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
            => new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> FailureResponse(string code, string message, object? details = null)
            => new() { Success = false, Message = message, Error = new ApiError { Code = code, Details = details } };
    }
}
