using DataIngestionService.Api.Models.Requests;
using FluentValidation;

namespace DataIngestionService.Api.Validators
{
    public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.TransactionDate)
                .NotNull()
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Transaction date cannot be in the future.");

            RuleFor(x => x.Amount)
                .NotNull()
                .GreaterThan(0);

            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .Matches("^[A-Z]{3}$")
                .WithMessage("Currency must be a valid 3-letter uppercase code.");

            RuleFor(x => x.SourceChannel)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}
