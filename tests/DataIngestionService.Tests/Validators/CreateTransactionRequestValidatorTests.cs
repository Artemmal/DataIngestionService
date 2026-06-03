using DataIngestionService.Api.Models.Requests;
using DataIngestionService.Api.Validators;
using FluentAssertions;

namespace DataIngestionService.Tests.Validators
{
    public sealed class CreateTransactionRequestValidatorTests
    {
        private readonly CreateTransactionRequestValidator _validator = new();

        [Fact]
        public void Validate_ShouldPass_WhenRequestIsValid()
        {
            // Arrange
            var request = new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Amount = 120.50m,
                Currency = "USD",
                SourceChannel = "Web"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_ShouldFail_WhenCustomerIdIsEmpty()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CustomerId = string.Empty;

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateTransactionRequest.CustomerId));
        }

        [Fact]
        public void Validate_ShouldFail_WhenAmountIsZero()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Amount = 0;

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateTransactionRequest.Amount));
        }

        [Fact]
        public void Validate_ShouldFail_WhenAmountIsNegative()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Amount = -10;

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateTransactionRequest.Amount));
        }

        [Theory]
        [InlineData("usd")]
        [InlineData("US")]
        [InlineData("USDD")]
        [InlineData("12A")]
        public void Validate_ShouldFail_WhenCurrencyIsInvalid(string currency)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Currency = currency;

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateTransactionRequest.Currency));
        }

        [Fact]
        public void Validate_ShouldFail_WhenTransactionDateIsInFuture()
        {
            // Arrange
            var request = CreateValidRequest();
            request.TransactionDate = DateTime.UtcNow.AddDays(1);

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateTransactionRequest.TransactionDate));
        }

        private static CreateTransactionRequest CreateValidRequest()
        {
            return new CreateTransactionRequest
            {
                CustomerId = "CUST-001",
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Amount = 120.50m,
                Currency = "USD",
                SourceChannel = "Web"
            };
        }
    }
}
