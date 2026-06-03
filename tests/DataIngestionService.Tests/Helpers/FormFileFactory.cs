using Microsoft.AspNetCore.Http;
using System.Text;

namespace DataIngestionService.Tests.Helpers
{
    public static class FormFileFactory
    {
        public static IFormFile CreateCsvFile(string content, string fileName = "transactions.csv")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);

            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
        }
    }
}
