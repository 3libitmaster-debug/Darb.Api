using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Darb.Api.DTOs.Base
{
    public class ResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        /// <summary>
        /// Generates a successful response with optional data.
        /// </summary>
        public static ResponseDto SuccessResponse(string message, object? data = default)
        {
            return new ResponseDto
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Generates a failure response for business logic errors (e.g., "Invalid Credentials").
        /// </summary>
        public static ResponseDto FailureResponse(string message)
        {
            return new ResponseDto
            {
                Success = false,
                Message = message,
                Data = null
            };
        }

        /// <summary>
        /// Generates a failure response for validation errors by extracting messages from ModelState.
        /// </summary>
        public static ResponseDto FailureResponse(string message, ModelStateDictionary modelState)
        {
            // Extract all error messages from the ModelState values
            var errors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return new ResponseDto
            {
                Success = false,
                Message = message,
                Data = errors // This will return a list of specific field errors to the client
            };
        }
    }
}