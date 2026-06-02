namespace DataIngestionService.Api.Models.Entities
{
    public sealed class Transaction
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string SourceChannel { get; set; } = null!;
        public string DeduplicationHash { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }
    }
}
