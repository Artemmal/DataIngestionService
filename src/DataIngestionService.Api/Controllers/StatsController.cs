using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DataIngestionService.Api.Controllers
{
    [ApiController]
    [Route("stats")]
    public sealed class StatsController : ControllerBase
    {
        private readonly IStatsService _statsService;

        public StatsController(IStatsService statsService)
        {
            _statsService = statsService;
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(StatsSummaryResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<StatsSummaryResponse>> GetSummary(
            CancellationToken cancellationToken)
        {
            var response = await _statsService.GetSummaryAsync(cancellationToken);

            return Ok(response);
        }
    }
}
