using DataIngestionService.Api.Models.Responses;

namespace DataIngestionService.Api.Services.Abstractions
{
    public interface IStatsService
    {
        Task<StatsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
    }
}
