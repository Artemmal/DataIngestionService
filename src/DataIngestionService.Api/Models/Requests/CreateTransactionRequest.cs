namespace DataIngestionService.Api.Models.Requests
{
    public sealed class CreateTransactionRequest
    {
        public string? CustomerId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? SourceChannel { get; set; }
    }
}