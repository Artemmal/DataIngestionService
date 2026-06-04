using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Persistence;
using DataIngestionService.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestionService.Api.Services
{
    public sealed class StatsService : IStatsService
    {
        private readonly AppDbContext _dbContext;

        public StatsService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StatsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
        {
            var totalTransactions = await _dbContext.Transactions
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var totalCustomers = await _dbContext.Transactions
                .AsNoTracking()
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken);

            var totalAmountByCurrency = await _dbContext.Transactions
                .AsNoTracking()
                .GroupBy(x => x.Currency)
                .Select(group => new AmountByCurrencyResponse
                {
                    Currency = group.Key,
                    TotalAmount = group.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Currency)
                .ToListAsync(cancellationToken);

            var transactionsBySourceChannel = await _dbContext.Transactions
                .AsNoTracking()
                .GroupBy(x => x.SourceChannel)
                .Select(group => new TransactionsBySourceChannelResponse
                {
                    SourceChannel = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.SourceChannel)
                .ToListAsync(cancellationToken);

            var latestTransactionDate = await _dbContext.Transactions
                .AsNoTracking()
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => (DateTime?)x.TransactionDate)
                .FirstOrDefaultAsync(cancellationToken);

            return new StatsSummaryResponse
            {
                TotalTransactions = totalTransactions,
                TotalCustomers = totalCustomers,
                TotalAmountByCurrency = totalAmountByCurrency,
                TransactionsBySourceChannel = transactionsBySourceChannel,
                LatestTransactionDate = latestTransactionDate
            };
        }
    }
}
