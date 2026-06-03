using DataIngestionService.Api.Exceptions;
using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Services.Abstractions;
using FluentValidation;
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
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Errors = ex.Errors.Select(error => new
                    {
                        Field = error.PropertyName,
                        Message = error.ErrorMessage
                    })
                });
            }
        }

        [HttpPost("batch")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BatchIngestionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BatchIngestionResponse>> IngestBatch(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file is null)
            {
                return BadRequest(new ErrorResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid file",
                    Detail = "CSV file is required."
                });
            }

            var response = await _transactionIngestionService.IngestBatchAsync(
                file,
                cancellationToken);

            return Ok(response);
        }
    }
}
