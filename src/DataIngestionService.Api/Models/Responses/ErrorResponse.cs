namespace DataIngestionService.Api.Models.Responses
{
    public sealed class ErrorResponse
    {
        public int Status { get; set; }
        public string Title { get; set; } = null!;
        public string? Detail { get; set; }
        public IReadOnlyCollection<ValidationErrorResponse>? Errors { get; set; }
    }

    public sealed class ValidationErrorResponse
    {
        public string Field { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
