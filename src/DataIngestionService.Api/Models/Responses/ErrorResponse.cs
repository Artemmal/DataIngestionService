namespace DataIngestionService.Api.Models.Responses
{
    public sealed class ErrorResponse
    {
        public int Status { get; set; }
        public string Title { get; set; } = null!;
        public string? Detail { get; set; }
    }
}
