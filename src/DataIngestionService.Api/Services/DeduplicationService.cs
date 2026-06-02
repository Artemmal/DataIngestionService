using DataIngestionService.Api.Models.Requests;
using System.Security.Cryptography;
using System.Text;

namespace DataIngestionService.Api.Services
{
    public sealed class DeduplicationService
    {
        public string GenerateHash(CreateTransactionRequest request)
        {
            var rawValue = string.Join("|",
                Normalize(request.CustomerId),
                request.TransactionDate?.ToUniversalTime().ToString("O"),
                request.Amount?.ToString("0.00"),
                Normalize(request.Currency),
                Normalize(request.SourceChannel));

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawValue));

            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Normalize(string? value)
        {
            return value?.Trim().ToUpperInvariant() ?? string.Empty;
        }
    }
}
