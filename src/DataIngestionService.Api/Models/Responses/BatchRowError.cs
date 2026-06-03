namespace DataIngestionService.Api.Models.Responses
{
    public sealed class BatchRowError
    {
        public int RowNumber { get; set; }
        public string? Field { get; set; }
        public string Message { get; set; } = null!;
    }
}
