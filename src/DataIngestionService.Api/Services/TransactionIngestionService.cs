using CsvHelper;
using CsvHelper.Configuration;
using DataIngestionService.Api.Exceptions;
using DataIngestionService.Api.Mapping;
using DataIngestionService.Api.Models.Csv;
using DataIngestionService.Api.Models.Entities;
using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;
using DataIngestionService.Api.Persistence;
using DataIngestionService.Api.Services.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;

namespace DataIngestionService.Api.Services
{
    public sealed class TransactionIngestionService : ITransactionIngestionService
    {
        private const int BatchSize = 1000;

        private readonly AppDbContext _dbContext;
        private readonly DeduplicationService _deduplicationService;
        private readonly IValidator<CreateTransactionRequest> _validator;

        public TransactionIngestionService(
            AppDbContext dbContext,
            DeduplicationService deduplicationService,
            IValidator<CreateTransactionRequest> validator)
        {
            _dbContext = dbContext;
            _deduplicationService = deduplicationService;
            _validator = validator;
        }

        public async Task<CreateTransactionResponse> CreateTransactionAsync(
            CreateTransactionRequest request,
            CancellationToken cancellationToken)
        {
            var normalizedRequest = NormalizeRequest(request);

            var validationResult = await _validator.ValidateAsync(normalizedRequest, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new FluentValidation.ValidationException(validationResult.Errors);
            }

            var deduplicationHash = _deduplicationService.GenerateHash(normalizedRequest);

            var duplicateExists = await _dbContext.Transactions
                .AnyAsync(x => x.DeduplicationHash == deduplicationHash, cancellationToken);

            if (duplicateExists)
            {
                throw new DuplicateTransactionException();
            }

            var transaction = CreateTransactionEntity(normalizedRequest, deduplicationHash);

            _dbContext.Transactions.Add(transaction);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                throw new DuplicateTransactionException();
            }

            return new CreateTransactionResponse
            {
                Id = transaction.Id,
                Status = "Accepted"
            };
        }

        public async Task<BatchIngestionResponse> IngestBatchAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var response = new BatchIngestionResponse();

            if (file.Length == 0)
            {
                response.Errors.Add(new BatchRowError
                {
                    RowNumber = 0,
                    Field = null,
                    Message = "CSV file is empty."
                });

                return response;
            }

            var transactionsToInsert = new List<Transaction>(BatchSize);
            var hashesInCurrentFile = new HashSet<string>();

            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, csvConfiguration);

            var rowNumber = 1;

            await foreach (var row in csv.GetRecordsAsync<TransactionCsvRow>(cancellationToken))
            {
                rowNumber++;
                response.TotalRows++;

                var isMapped = TransactionCsvMapper.TryMap(
                    row,
                    rowNumber,
                    out var request,
                    out var mappingErrors);

                if (!isMapped)
                {
                    response.Errors.AddRange(mappingErrors);
                    response.RejectedRows++;
                    continue;
                }

                var normalizedRequest = NormalizeRequest(request);

                var validationResult = await _validator.ValidateAsync(normalizedRequest, cancellationToken);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        response.Errors.Add(new BatchRowError
                        {
                            RowNumber = rowNumber,
                            Field = error.PropertyName,
                            Message = error.ErrorMessage
                        });
                    }

                    response.RejectedRows++;
                    continue;
                }

                var deduplicationHash = _deduplicationService.GenerateHash(normalizedRequest);

                if (!hashesInCurrentFile.Add(deduplicationHash))
                {
                    response.Errors.Add(new BatchRowError
                    {
                        RowNumber = rowNumber,
                        Field = null,
                        Message = "Duplicate transaction inside the uploaded file."
                    });

                    response.RejectedRows++;
                    continue;
                }

                var duplicateExists = await _dbContext.Transactions
                    .AnyAsync(x => x.DeduplicationHash == deduplicationHash, cancellationToken);

                if (duplicateExists)
                {
                    response.Errors.Add(new BatchRowError
                    {
                        RowNumber = rowNumber,
                        Field = null,
                        Message = "Duplicate transaction already exists."
                    });

                    response.RejectedRows++;
                    continue;
                }

                transactionsToInsert.Add(CreateTransactionEntity(normalizedRequest, deduplicationHash));

                if (transactionsToInsert.Count >= BatchSize)
                {
                    await InsertTransactionsAsync(transactionsToInsert, cancellationToken);
                    response.AcceptedRows += transactionsToInsert.Count;
                    transactionsToInsert.Clear();
                }
            }

            if (transactionsToInsert.Count > 0)
            {
                await InsertTransactionsAsync(transactionsToInsert, cancellationToken);
                response.AcceptedRows += transactionsToInsert.Count;
            }

            return response;
        }

        private async Task InsertTransactionsAsync(
            List<Transaction> transactions,
            CancellationToken cancellationToken)
        {
            _dbContext.Transactions.AddRange(transactions);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                throw new DuplicateTransactionException();
            }

            _dbContext.ChangeTracker.Clear();
        }

        private static CreateTransactionRequest NormalizeRequest(CreateTransactionRequest request)
        {
            return new CreateTransactionRequest
            {
                CustomerId = request.CustomerId?.Trim(),
                TransactionDate = request.TransactionDate,
                Amount = request.Amount,
                Currency = request.Currency?.Trim().ToUpperInvariant(),
                SourceChannel = request.SourceChannel?.Trim()
            };
        }

        private static Transaction CreateTransactionEntity(
            CreateTransactionRequest request,
            string deduplicationHash)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId!,
                TransactionDate = request.TransactionDate!.Value.ToUniversalTime(),
                Amount = request.Amount!.Value,
                Currency = request.Currency!,
                SourceChannel = request.SourceChannel!,
                DeduplicationHash = deduplicationHash,
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException postgresException &&
                   postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
        }
    }
}
