namespace DataIngestionService.Api.Models.Responses
{
    public sealed class BatchIngestionResponse
    {
        public int TotalRows { get; set; }
        public int AcceptedRows { get; set; }
        public int RejectedRows { get; set; }
        public List<BatchRowError> Errors { get; set; } = [];
    }
}
