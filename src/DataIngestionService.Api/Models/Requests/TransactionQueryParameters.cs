namespace DataIngestionService.Api.Models.Requests
{
    public sealed class TransactionQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Currency { get; set; }
        public string? SourceChannel { get; set; }
    }
}
