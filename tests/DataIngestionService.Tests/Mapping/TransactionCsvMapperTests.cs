using DataIngestionService.Api.Mapping;
using DataIngestionService.Api.Models.Csv;
using FluentAssertions;

namespace DataIngestionService.Tests.Mapping
{
    public sealed class TransactionCsvMapperTests
    {
        [Fact]
        public void TryMap_ShouldReturnRequest_WhenCsvRowIsValid()
        {
            // Arrange
            var row = new TransactionCsvRow
            {
                CustomerId = "CUST-001",
                TransactionDate = "2025-06-01T10:20:00Z",
                Amount = "120.50",
                Currency = "USD",
                SourceChannel = "Web"
            };

            // Act
            var result = TransactionCsvMapper.TryMap(
                row,
                rowNumber: 2,
                out var request,
                out var errors);

            // Assert
            result.Should().BeTrue();
            errors.Should().BeEmpty();

            request.CustomerId.Should().Be("CUST-001");
            request.TransactionDate.Should().NotBeNull();
            request.Amount.Should().Be(120.50m);
            request.Currency.Should().Be("USD");
            request.SourceChannel.Should().Be("Web");
        }

        [Fact]
        public void TryMap_ShouldReturnError_WhenTransactionDateIsInvalid()
        {
            // Arrange
            var row = new TransactionCsvRow
            {
                CustomerId = "CUST-001",
                TransactionDate = "wrong-date",
                Amount = "120.50",
                Currency = "USD",
                SourceChannel = "Web"
            };

            // Act
            var result = TransactionCsvMapper.TryMap(
                row,
                rowNumber: 2,
                out _,
                out var errors);

            // Assert
            result.Should().BeFalse();

            errors.Should().ContainSingle(error =>
                error.RowNumber == 2 &&
                error.Field == nameof(TransactionCsvRow.TransactionDate) &&
                error.Message == "Transaction date is invalid.");
        }

        [Fact]
        public void TryMap_ShouldReturnError_WhenAmountIsInvalid()
        {
            // Arrange
            var row = new TransactionCsvRow
            {
                CustomerId = "CUST-001",
                TransactionDate = "2025-06-01T10:20:00Z",
                Amount = "abc",
                Currency = "USD",
                SourceChannel = "Web"
            };

            // Act
            var result = TransactionCsvMapper.TryMap(
                row,
                rowNumber: 2,
                out _,
                out var errors);

            // Assert
            result.Should().BeFalse();

            errors.Should().ContainSingle(error =>
                error.RowNumber == 2 &&
                error.Field == nameof(TransactionCsvRow.Amount) &&
                error.Message == "Amount is invalid.");
        }

        [Fact]
        public void TryMap_ShouldReturnMultipleErrors_WhenDateAndAmountAreInvalid()
        {
            // Arrange
            var row = new TransactionCsvRow
            {
                CustomerId = "CUST-001",
                TransactionDate = "wrong-date",
                Amount = "wrong-amount",
                Currency = "USD",
                SourceChannel = "Web"
            };

            // Act
            var result = TransactionCsvMapper.TryMap(
                row,
                rowNumber: 2,
                out _,
                out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().HaveCount(2);
            errors.Should().Contain(error => error.Field == nameof(TransactionCsvRow.TransactionDate));
            errors.Should().Contain(error => error.Field == nameof(TransactionCsvRow.Amount));
        }
    }
}
