using System.Net;

namespace gezzyn.Domain.DTO
{
    public class Response<T>
    {
        public string Status { get; set; } = "Success";
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public T? Data { get; set; }

        public static Response<T> Success(T data, string? message = null)
        {
            return new Response<T>
            {
                Status = "Success",
                StatusCode = HttpStatusCode.OK,
                Message = message,
                Data = data,
                Errors = new List<string>()
            };
        }

        public static Response<T> Error(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        {
            return new Response<T>
            {
                Status = "Error",
                StatusCode = statusCode,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string>()
            };
        }

        public static Response<T> ValidationError(List<string> errors, string? message = "Validation failed")
        {
            return new Response<T>
            {
                Status = "Error",
                StatusCode = HttpStatusCode.BadRequest,
                Message = message,
                Data = default,
                Errors = errors
            };
        }
    }
}
