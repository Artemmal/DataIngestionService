using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;

namespace DataIngestionService.Api.Services.Abstractions
{
    public interface ITransactionQueryService
    {
        Task<PaginatedResponse<TransactionResponse>> GetCustomerTransactionsAsync(
            string customerId,
            TransactionQueryParameters queryParameters,
            CancellationToken cancellationToken);
    }
}
