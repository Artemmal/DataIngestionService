using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Services;
using DataIngestionService.Api.Validators;
using DataIngestionService.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestionService.Tests.Services
{
    public sealed class TransactionIngestionServiceBatchTests
    {
        [Fact]
        public async Task IngestBatchAsync_ShouldSaveValidRows()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var validator = new CreateTransactionRequestValidator();

            var service = new TransactionIngestionService(
                dbContext,
                deduplicationService,
                validator);

            var csv = """
                  CustomerId,TransactionDate,Amount,Currency,SourceChannel
                  CUST-001,2025-06-01T10:20:00Z,120.50,USD,Web
                  CUST-002,2025-06-01T11:15:00Z,89.99,EUR,Mobile
                  """;

            var file = FormFileFactory.CreateCsvFile(csv);

            // Act
            var response = await service.IngestBatchAsync(file, CancellationToken.None);

            // Assert
            response.TotalRows.Should().Be(2);
            response.AcceptedRows.Should().Be(2);
            response.RejectedRows.Should().Be(0);
            response.Errors.Should().BeEmpty();

            var savedTransactions = await dbContext.Transactions.ToListAsync();

            savedTransactions.Should().HaveCount(2);
            savedTransactions.Should().Contain(x => x.CustomerId == "CUST-001");
            savedTransactions.Should().Contain(x => x.CustomerId == "CUST-002");
        }

        [Fact]
        public async Task IngestBatchAsync_ShouldRejectRow_WhenAmountIsNegative()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var validator = new CreateTransactionRequestValidator();

            var service = new TransactionIngestionService(
                dbContext,
                deduplicationService,
                validator);

            var csv = """
                  CustomerId,TransactionDate,Amount,Currency,SourceChannel
                  CUST-001,2025-06-01T10:20:00Z,-120.50,USD,Web
                  """;

            var file = FormFileFactory.CreateCsvFile(csv);

            // Act
            var response = await service.IngestBatchAsync(file, CancellationToken.None);

            // Assert
            response.TotalRows.Should().Be(1);
            response.AcceptedRows.Should().Be(0);
            response.RejectedRows.Should().Be(1);

            response.Errors.Should().ContainSingle(error =>
                error.RowNumber == 2 &&
                error.Field == nameof(CreateTransactionRequest.Amount));

            var savedTransactionsCount = await dbContext.Transactions.CountAsync();
            savedTransactionsCount.Should().Be(0);
        }

        [Fact]
        public async Task IngestBatchAsync_ShouldRejectRow_WhenDateIsInvalid()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var validator = new CreateTransactionRequestValidator();

            var service = new TransactionIngestionService(
                dbContext,
                deduplicationService,
                validator);

            var csv = """
                  CustomerId,TransactionDate,Amount,Currency,SourceChannel
                  CUST-001,wrong-date,120.50,USD,Web
                  """;

            var file = FormFileFactory.CreateCsvFile(csv);

            // Act
            var response = await service.IngestBatchAsync(file, CancellationToken.None);

            // Assert
            response.TotalRows.Should().Be(1);
            response.AcceptedRows.Should().Be(0);
            response.RejectedRows.Should().Be(1);

            response.Errors.Should().ContainSingle(error =>
                error.RowNumber == 2 &&
                error.Field == "TransactionDate" &&
                error.Message == "Transaction date is invalid.");

            var savedTransactionsCount = await dbContext.Transactions.CountAsync();
            savedTransactionsCount.Should().Be(0);
        }

        [Fact]
        public async Task IngestBatchAsync_ShouldRejectDuplicateInsideUploadedFile()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var validator = new CreateTransactionRequestValidator();

            var service = new TransactionIngestionService(
                dbContext,
                deduplicationService,
                validator);

            var csv = """
                  CustomerId,TransactionDate,Amount,Currency,SourceChannel
                  CUST-001,2025-06-01T10:20:00Z,120.50,USD,Web
                  CUST-001,2025-06-01T10:20:00Z,120.50,USD,Web
                  """;

            var file = FormFileFactory.CreateCsvFile(csv);

            // Act
            var response = await service.IngestBatchAsync(file, CancellationToken.None);

            // Assert
            response.TotalRows.Should().Be(2);
            response.AcceptedRows.Should().Be(1);
            response.RejectedRows.Should().Be(1);

            response.Errors.Should().ContainSingle(error =>
                error.RowNumber == 3 &&
                error.Field == null &&
                error.Message == "Duplicate transaction inside the uploaded file.");

            var savedTransactionsCount = await dbContext.Transactions.CountAsync();
            savedTransactionsCount.Should().Be(1);
        }

        [Fact]
        public async Task IngestBatchAsync_ShouldRejectDuplicateAlreadyExistingInDatabase()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var validator = new CreateTransactionRequestValidator();

            var service = new TransactionIngestionService(
                dbContext,
                deduplicationService,
                validator);

            var existingRequest = new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = new DateTime(2025, 06, 01, 10, 20, 00, DateTimeKind.Utc),
                Amount = 120.50m,
                Currency = "USD",
                SourceChannel = "Web"
            };

            await service.CreateTransactionAsync(existingRequest, CancellationToken.None);

            var csv = """
                  CustomerId,TransactionDate,Amount,Currency,SourceChannel
                  CUST-001,2025-06-01T10:20:00Z,120.50,USD,Web
                  """;

            var file = FormFileFactory.CreateCsvFile(csv);

            // Act
            var response = await service.IngestBatchAsync(file, CancellationToken.None);

            // Assert
            response.TotalRows.Should().Be(1);
            response.AcceptedRows.Should().Be(0);
            response.RejectedRows.Should().Be(1);

            response.Errors.Should().ContainSingle(error =>
                error.RowNumber == 2 &&
                error.Field == null &&
                error.Message == "Duplicate transaction already exists.");

            var savedTransactionsCount = await dbContext.Transactions.CountAsync();
            savedTransactionsCount.Should().Be(1);
        }

        [Fact]
        public async Task IngestBatchAsync_ShouldReturnError_WhenFileIsEmpty()
        {
            // Arrange
            await using var dbContext = DbContextFactory.Create();

            var deduplicationService = new DeduplicationService();
            var validator = new CreateTransactionRequestValidator();

            var service = new TransactionIngestionService(
                dbContext,
                deduplicationService,
                validator);

            var file = FormFileFactory.CreateCsvFile(string.Empty);

            // Act
            var response = await service.IngestBatchAsync(file, CancellationToken.None);

            // Assert
            response.TotalRows.Should().Be(0);
            response.AcceptedRows.Should().Be(0);
            response.RejectedRows.Should().Be(0);

            response.Errors.Should().ContainSingle(error =>
                error.RowNumber == 0 &&
                error.Field == null &&
                error.Message == "CSV file is empty.");
        }
    }
}
