using DataIngestionService.Api.Models.Csv;
using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Models.Responses;

namespace DataIngestionService.Api.Mapping
{
    public static class TransactionCsvMapper
    {
        public static bool TryMap(
            TransactionCsvRow row,
            int rowNumber,
            out CreateTransactionRequest request,
            out List<BatchRowError> errors)
        {
            errors = [];
            request = new CreateTransactionRequest
            {
                CustomerId = row.CustomerId,
                Currency = row.Currency,
                SourceChannel = row.SourceChannel
            };

            if (DateTime.TryParse(row.TransactionDate, out var transactionDate))
            {
                request.TransactionDate = DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc);
            }
            else
            {
                errors.Add(new BatchRowError
                {
                    RowNumber = rowNumber,
                    Field = nameof(TransactionCsvRow.TransactionDate),
                    Message = "Transaction date is invalid."
                });
            }

            if (decimal.TryParse(row.Amount, out var amount))
            {
                request.Amount = amount;
            }
            else
            {
                errors.Add(new BatchRowError
                {
                    RowNumber = rowNumber,
                    Field = nameof(TransactionCsvRow.Amount),
                    Message = "Amount is invalid."
                });
            }

            return errors.Count == 0;
        }
    }
}
