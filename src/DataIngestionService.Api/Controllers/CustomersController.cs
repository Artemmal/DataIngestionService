using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DataIngestionService.Api.Controllers
{
    [ApiController]
    [Route("customers")]
    public sealed class CustomersController : ControllerBase
    {
        private readonly ITransactionQueryService _transactionQueryService;

        public CustomersController(ITransactionQueryService transactionQueryService)
        {
            _transactionQueryService = transactionQueryService;
        }

        [HttpGet("{id}/transactions")]
        [ProducesResponseType(typeof(PaginatedResponse<TransactionResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<TransactionResponse>>> GetCustomerTransactions(
            string id,
            [FromQuery] TransactionQueryParameters queryParameters,
            CancellationToken cancellationToken)
        {
            var response = await _transactionQueryService.GetCustomerTransactionsAsync(
                id,
                queryParameters,
                cancellationToken);

            return Ok(response);
        }
    }
}
