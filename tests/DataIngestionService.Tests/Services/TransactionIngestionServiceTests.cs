using DataIngestionService.Api.Exceptions;
using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Services;
using DataIngestionService.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestionService.Tests.Services
{
    public sealed class TransactionIngestionServiceTests
    {
        [Fact]
        public async Task CreateTransactionAsync_ShouldSaveTransaction_WhenRequestIsValid()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var service = new TransactionIngestionService(dbContext, deduplicationService);

            var request = CreateRequest();

            // Act
            var response = await service.CreateTransactionAsync(request, CancellationToken.None);

            // Assert
            response.Id.Should().NotBeEmpty();
            response.Status.Should().Be("Accepted");

            var savedTransaction = await dbContext.Transactions.SingleAsync();

            savedTransaction.Id.Should().Be(response.Id);
            savedTransaction.CustomerId.Should().Be("CUST-001");
            savedTransaction.Amount.Should().Be(120.50m);
            savedTransaction.Currency.Should().Be("USD");
            savedTransaction.SourceChannel.Should().Be("Web");
            savedTransaction.DeduplicationHash.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task CreateTransactionAsync_ShouldNormalizeStoredValues()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var service = new TransactionIngestionService(dbContext, deduplicationService);

            var request = new CreateTransactionRequest
            {
                CustomerId = " CUST-001 ",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = 120.50m,
                Currency = " usd ",
                SourceChannel = " Web "
            };

            // Act
            var response = await service.CreateTransactionAsync(request, CancellationToken.None);

            // Assert
            response.Status.Should().Be("Accepted");

            var savedTransaction = await dbContext.Transactions.SingleAsync();

            savedTransaction.CustomerId.Should().Be("CUST-001");
            savedTransaction.Currency.Should().Be("USD");
            savedTransaction.SourceChannel.Should().Be("Web");
        }

        [Fact]
        public async Task CreateTransactionAsync_ShouldThrowDuplicateTransactionException_WhenDuplicateExists()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var service = new TransactionIngestionService(dbContext, deduplicationService);

            var request = CreateRequest();

            await service.CreateTransactionAsync(request, CancellationToken.None);

            // Act
            var act = async () => await service.CreateTransactionAsync(request, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DuplicateTransactionException>();

            var transactionsCount = await dbContext.Transactions.CountAsync();
            transactionsCount.Should().Be(1);
        }

        private static CreateTransactionRequest CreateRequest()
        {
            return new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = 120.50m,
                Currency = "USD",
                SourceChannel = "Web"
            };
        }
    }
}
