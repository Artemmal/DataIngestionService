namespace DataIngestionService.Api.Models.Responses
{
    public sealed class CreateTransactionResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = null!;
    }
}
