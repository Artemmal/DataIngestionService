using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Services;
using FluentAssertions;

namespace DataIngestionService.Tests.Services
{
    public sealed class DeduplicationServiceTests
    {
        private readonly DeduplicationService _deduplicationService = new();

        [Fact]
        public void GenerateHash_ShouldReturnSameHash_ForSameTransactionData()
        {
            // Arrange
            var firstRequest = new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = 120.50m,
                Currency = "USD",
                SourceChannel = "Web"
            };

            var secondRequest = new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = 120.50m,
                Currency = "USD",
                SourceChannel = "Web"
            };

            // Act
            var firstHash = _deduplicationService.GenerateHash(firstRequest);
            var secondHash = _deduplicationService.GenerateHash(secondRequest);

            // Assert
            firstHash.Should().Be(secondHash);
        }

        [Fact]
        public void GenerateHash_ShouldNormalizeStringFields()
        {
            // Arrange
            var firstRequest = new CreateTransactionRequest
            {
                CustomerId = " cust-001 ",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = 120.50m,
                Currency = " usd ",
                SourceChannel = " web "
            };

            var secondRequest = new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = 120.50m,
                Currency = "USD",
                SourceChannel = "WEB"
            };

            // Act
            var firstHash = _deduplicationService.GenerateHash(firstRequest);
            var secondHash = _deduplicationService.GenerateHash(secondRequest);

            // Assert
            firstHash.Should().Be(secondHash);
        }

        [Fact]
        public void GenerateHash_ShouldReturnDifferentHash_WhenAmountIsDifferent()
        {
            // Arrange
            var firstRequest = CreateRequest(amount: 120.50m);
            var secondRequest = CreateRequest(amount: 130.50m);

            // Act
            var firstHash = _deduplicationService.GenerateHash(firstRequest);
            var secondHash = _deduplicationService.GenerateHash(secondRequest);

            // Assert
            firstHash.Should().NotBe(secondHash);
        }

        [Fact]
        public void GenerateHash_ShouldReturnSha256Hash()
        {
            // Arrange
            var request = CreateRequest(amount: 120.50m);

            // Act
            var hash = _deduplicationService.GenerateHash(request);

            // Assert
            hash.Should().HaveLength(64);
        }

        private static CreateTransactionRequest CreateRequest(decimal amount)
        {
            return new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = amount,
                Currency = "USD",
                SourceChannel = "Web"
            };
        }
    }
}
