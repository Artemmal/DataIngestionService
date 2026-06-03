namespace DataIngestionService.Api.Models.Csv
{
    public sealed class TransactionCsvRow
    {
        public string? CustomerId { get; set; }
        public string? TransactionDate { get; set; }
        public string? Amount { get; set; }
        public string? Currency { get; set; }
        public string? SourceChannel { get; set; }
    }
}
