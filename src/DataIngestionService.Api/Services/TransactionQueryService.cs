using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Persistence;
using DataIngestionService.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestionService.Api.Services
{
    public sealed class TransactionQueryService : ITransactionQueryService
    {
        private const int MaxPageSize = 100;

        private readonly AppDbContext _dbContext;

        public TransactionQueryService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaginatedResponse<TransactionResponse>> GetCustomerTransactionsAsync(
            string customerId,
            TransactionQueryParameters queryParameters,
            CancellationToken cancellationToken)
        {
            var page = queryParameters.Page < 1 ? 1 : queryParameters.Page;
            var pageSize = queryParameters.PageSize switch
            {
                < 1 => 20,
                > MaxPageSize => MaxPageSize,
                _ => queryParameters.PageSize
            };

            var normalizedCustomerId = customerId.Trim();

            var query = _dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.CustomerId == normalizedCustomerId);

            if (queryParameters.From.HasValue)
            {
                var from = queryParameters.From.Value.ToUniversalTime();
                query = query.Where(x => x.TransactionDate >= from);
            }

            if (queryParameters.To.HasValue)
            {
                var to = queryParameters.To.Value.ToUniversalTime();
                query = query.Where(x => x.TransactionDate <= to);
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.Currency))
            {
                var currency = queryParameters.Currency.Trim().ToUpperInvariant();
                query = query.Where(x => x.Currency == currency);
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.SourceChannel))
            {
                var sourceChannel = queryParameters.SourceChannel.Trim();
                query = query.Where(x => x.SourceChannel == sourceChannel);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.TransactionDate)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TransactionResponse
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    TransactionDate = x.TransactionDate,
                    Amount = x.Amount,
                    Currency = x.Currency,
                    SourceChannel = x.SourceChannel,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<TransactionResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}
