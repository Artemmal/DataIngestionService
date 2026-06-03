using DataIngestionService.Api.Exceptions;
using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DataIngestionService.Api.Controllers
{
    [ApiController]
    [Route("ingest")]
    public sealed class IngestionController : ControllerBase
    {
        private readonly ITransactionIngestionService _transactionIngestionService;

        public IngestionController(ITransactionIngestionService transactionIngestionService)
        {
            _transactionIngestionService = transactionIngestionService;
        }

        [HttpPost("transaction")]
        [ProducesResponseType(typeof(CreateTransactionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CreateTransactionResponse>> CreateTransaction(
            [FromBody] CreateTransactionRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _transactionIngestionService.CreateTransactionAsync(
                    request,
                    cancellationToken);

                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (DuplicateTransactionException ex)
            {
                return Conflict(new ErrorResponse
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate transaction",
                    Detail = ex.Message
                });
            }
        }
    }
}
