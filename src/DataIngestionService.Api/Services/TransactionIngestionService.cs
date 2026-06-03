using DataIngestionService.Api.Exceptions;
using DataIngestionService.Api.Models.Entities;
using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Persistence;
using DataIngestionService.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestionService.Api.Services
{
    public sealed class TransactionIngestionService : ITransactionIngestionService
    {
        private readonly AppDbContext _dbContext;
        private readonly DeduplicationService _deduplicationService;

        public TransactionIngestionService(
            AppDbContext dbContext,
            DeduplicationService deduplicationService)
        {
            _dbContext = dbContext;
            _deduplicationService = deduplicationService;
        }

        public async Task<CreateTransactionResponse> CreateTransactionAsync(
            CreateTransactionRequest request,
            CancellationToken cancellationToken)
        {
            var deduplicationHash = _deduplicationService.GenerateHash(request);

            var duplicateExists = await _dbContext.Transactions
                .AnyAsync(x => x.DeduplicationHash == deduplicationHash, cancellationToken);

            if (duplicateExists)
            {
                throw new DuplicateTransactionException();
            }

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId!.Trim(),
                TransactionDate = request.TransactionDate!.Value.ToUniversalTime(),
                Amount = request.Amount!.Value,
                Currency = request.Currency!.Trim().ToUpperInvariant(),
                SourceChannel = request.SourceChannel!.Trim(),
                DeduplicationHash = deduplicationHash,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(transaction);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                if (await IsDuplicateAsync(deduplicationHash, cancellationToken))
                {
                    throw new DuplicateTransactionException();
                }
                throw;
            }

            return new CreateTransactionResponse
            {
                Id = transaction.Id,
                Status = "Accepted"
            };
        }

        private async Task<bool> IsDuplicateAsync(string deduplicationHash, CancellationToken cancellationToken)
        {
            return await _dbContext.Transactions
                .AnyAsync(x => x.DeduplicationHash == deduplicationHash, cancellationToken);
        }
    }
}
