using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;

namespace DataIngestionService.Api.Services.Abstractions
{
    public interface ITransactionIngestionService
    {
        Task<CreateTransactionResponse> CreateTransactionAsync(
            CreateTransactionRequest request,
            CancellationToken cancellationToken);
    }
}
