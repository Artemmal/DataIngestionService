namespace DataIngestionService.Api.Models.Responses
{
    public sealed class StatsSummaryResponse
    {
        public int TotalTransactions { get; set; }
        public int TotalCustomers { get; set; }
        public IReadOnlyCollection<AmountByCurrencyResponse> TotalAmountByCurrency { get; set; } = [];
        public IReadOnlyCollection<TransactionsBySourceChannelResponse> TransactionsBySourceChannel { get; set; } = [];
        public DateTime? LatestTransactionDate { get; set; }
    }

    public sealed class AmountByCurrencyResponse
    {
        public string Currency { get; set; } = null!;
        public decimal TotalAmount { get; set; }
    }

    public sealed class TransactionsBySourceChannelResponse
    {
        public string SourceChannel { get; set; } = null!;
        public int Count { get; set; }
    }
}
