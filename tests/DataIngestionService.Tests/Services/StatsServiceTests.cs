using DataIngestionService.Api.Models.Entities;
using DataIngestionService.Api.Services;
using DataIngestionService.Tests.Helpers;
using FluentAssertions;

namespace DataIngestionService.Tests.Services
{
    public sealed class StatsServiceTests
    {
        [Fact]
        public async Task GetSummaryAsync_ShouldReturnEmptySummary_WhenThereAreNoTransactions()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var service = new StatsService(dbContext);

            // Act
            var response = await service.GetSummaryAsync(CancellationToken.None);

            // Assert
            response.TotalTransactions.Should().Be(0);
            response.TotalCustomers.Should().Be(0);
            response.TotalAmountByCurrency.Should().BeEmpty();
            response.TransactionsBySourceChannel.Should().BeEmpty();
            response.LatestTransactionDate.Should().BeNull();
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldReturnTotalTransactionsAndCustomers()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction("CUST-001", 100, "USD", "Web"),
                CreateTransaction("CUST-001", 200, "USD", "Mobile"),
                CreateTransaction("CUST-002", 300, "EUR", "Web"));

            await dbContext.SaveChangesAsync();

            var service = new StatsService(dbContext);

            // Act
            var response = await service.GetSummaryAsync(CancellationToken.None);

            // Assert
            response.TotalTransactions.Should().Be(3);
            response.TotalCustomers.Should().Be(2);
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldReturnTotalAmountByCurrency()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction("CUST-001", 100, "USD", "Web"),
                CreateTransaction("CUST-002", 200, "USD", "Mobile"),
                CreateTransaction("CUST-003", 300, "EUR", "Web"));

            await dbContext.SaveChangesAsync();

            var service = new StatsService(dbContext);

            // Act
            var response = await service.GetSummaryAsync(CancellationToken.None);

            // Assert
            response.TotalAmountByCurrency.Should().ContainSingle(x =>
                x.Currency == "USD" &&
                x.TotalAmount == 300);

            response.TotalAmountByCurrency.Should().ContainSingle(x =>
                x.Currency == "EUR" &&
                x.TotalAmount == 300);
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldReturnTransactionsBySourceChannel()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction("CUST-001", 100, "USD", "Web"),
                CreateTransaction("CUST-002", 200, "USD", "Web"),
                CreateTransaction("CUST-003", 300, "EUR", "Mobile"));

            await dbContext.SaveChangesAsync();

            var service = new StatsService(dbContext);

            // Act
            var response = await service.GetSummaryAsync(CancellationToken.None);

            // Assert
            response.TransactionsBySourceChannel.Should().ContainSingle(x =>
                x.SourceChannel == "Web" &&
                x.Count == 2);

            response.TransactionsBySourceChannel.Should().ContainSingle(x =>
                x.SourceChannel == "Mobile" &&
                x.Count == 1);
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldReturnLatestTransactionDate()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var latestDate = new DateTime(2025, 12, 01, 10, 00, 00, DateTimeKind.Utc);

            dbContext.Transactions.AddRange(
                CreateTransaction(
                    "CUST-001",
                    100,
                    "USD",
                    "Web",
                    new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc)),
                CreateTransaction(
                    "CUST-002",
                    200,
                    "EUR",
                    "Mobile",
                    latestDate),
                CreateTransaction(
                    "CUST-003",
                    300,
                    "USD",
                    "POS",
                    new DateTime(2025, 06, 01, 10, 00, 00, DateTimeKind.Utc)));

            await dbContext.SaveChangesAsync();

            var service = new StatsService(dbContext);

            // Act
            var response = await service.GetSummaryAsync(CancellationToken.None);

            // Assert
            response.LatestTransactionDate.Should().Be(latestDate);
        }

        private static Transaction CreateTransaction(
            string customerId,
            decimal amount,
            string currency,
            string sourceChannel,
            DateTime? transactionDate = null)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TransactionDate = transactionDate ?? DateTime.UtcNow,
                Amount = amount,
                Currency = currency,
                SourceChannel = sourceChannel,
                DeduplicationHash = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}
