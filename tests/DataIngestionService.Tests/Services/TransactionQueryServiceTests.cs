using DataIngestionService.Api.Models.Entities;
using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Services;
using DataIngestionService.Tests.Helpers;
using FluentAssertions;

namespace DataIngestionService.Tests.Services
{
    public sealed class TransactionQueryServiceTests
    {
        [Fact]
        public async Task GetCustomerTransactionsAsync_ShouldReturnOnlyTransactionsForRequestedCustomer()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction("CUST-001", 100, "USD", "Web"),
                CreateTransaction("CUST-001", 200, "EUR", "Mobile"),
                CreateTransaction("CUST-002", 300, "USD", "Web"));

            await dbContext.SaveChangesAsync();

            var service = new TransactionQueryService(dbContext);

            var queryParameters = new TransactionQueryParameters();

            // Act
            var response = await service.GetCustomerTransactionsAsync(
                "CUST-001",
                queryParameters,
                CancellationToken.None);

            // Assert
            response.TotalCount.Should().Be(2);
            response.Items.Should().HaveCount(2);
            response.Items.Should().OnlyContain(x => x.CustomerId == "CUST-001");
        }

        [Fact]
        public async Task GetCustomerTransactionsAsync_ShouldApplyCurrencyFilter()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction("CUST-001", 100, "USD", "Web"),
                CreateTransaction("CUST-001", 200, "EUR", "Mobile"));

            await dbContext.SaveChangesAsync();

            var service = new TransactionQueryService(dbContext);

            var queryParameters = new TransactionQueryParameters
            {
                Currency = "usd"
            };

            // Act
            var response = await service.GetCustomerTransactionsAsync(
                "CUST-001",
                queryParameters,
                CancellationToken.None);

            // Assert
            response.TotalCount.Should().Be(1);
            response.Items.Should().ContainSingle(x => x.Currency == "USD");
        }

        [Fact]
        public async Task GetCustomerTransactionsAsync_ShouldApplySourceChannelFilter()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction("CUST-001", 100, "USD", "Web"),
                CreateTransaction("CUST-001", 200, "USD", "Mobile"));

            await dbContext.SaveChangesAsync();

            var service = new TransactionQueryService(dbContext);

            var queryParameters = new TransactionQueryParameters
            {
                SourceChannel = "Mobile"
            };

            // Act
            var response = await service.GetCustomerTransactionsAsync(
                "CUST-001",
                queryParameters,
                CancellationToken.None);

            // Assert
            response.TotalCount.Should().Be(1);
            response.Items.Should().ContainSingle(x => x.SourceChannel == "Mobile");
        }

        [Fact]
        public async Task GetCustomerTransactionsAsync_ShouldApplyDateRangeFilter()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction(
                    "CUST-001",
                    100,
                    "USD",
                    "Web",
                    new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc)),
                CreateTransaction(
                    "CUST-001",
                    200,
                    "USD",
                    "Web",
                    new DateTime(2025, 06, 01, 10, 00, 00, DateTimeKind.Utc)),
                CreateTransaction(
                    "CUST-001",
                    300,
                    "USD",
                    "Web",
                    new DateTime(2025, 12, 01, 10, 00, 00, DateTimeKind.Utc)));

            await dbContext.SaveChangesAsync();

            var service = new TransactionQueryService(dbContext);

            var queryParameters = new TransactionQueryParameters
            {
                From = new DateTime(2025, 05, 01, 00, 00, 00, DateTimeKind.Utc),
                To = new DateTime(2025, 07, 01, 00, 00, 00, DateTimeKind.Utc)
            };

            // Act
            var response = await service.GetCustomerTransactionsAsync(
                "CUST-001",
                queryParameters,
                CancellationToken.None);

            // Assert
            response.TotalCount.Should().Be(1);
            response.Items.Should().ContainSingle(x => x.Amount == 200);
        }

        [Fact]
        public async Task GetCustomerTransactionsAsync_ShouldApplyPagination()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            dbContext.Transactions.AddRange(
                CreateTransaction("CUST-001", 100, "USD", "Web"),
                CreateTransaction("CUST-001", 200, "USD", "Web"),
                CreateTransaction("CUST-001", 300, "USD", "Web"));

            await dbContext.SaveChangesAsync();

            var service = new TransactionQueryService(dbContext);

            var queryParameters = new TransactionQueryParameters
            {
                Page = 2,
                PageSize = 2
            };

            // Act
            var response = await service.GetCustomerTransactionsAsync(
                "CUST-001",
                queryParameters,
                CancellationToken.None);

            // Assert
            response.TotalCount.Should().Be(3);
            response.TotalPages.Should().Be(2);
            response.Page.Should().Be(2);
            response.PageSize.Should().Be(2);
            response.Items.Should().HaveCount(1);
            response.HasPreviousPage.Should().BeTrue();
            response.HasNextPage.Should().BeFalse();
        }

        private static Transaction CreateTransaction(
            string customerId,
            decimal amount,
            string currency,
            string sourceChannel,
            DateTime? transactionDate = null)
        {
            var date = transactionDate ?? DateTime.UtcNow;

            return new Transaction
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TransactionDate = date,
                Amount = amount,
                Currency = currency,
                SourceChannel = sourceChannel,
                DeduplicationHash = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}
