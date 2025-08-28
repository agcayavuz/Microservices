using BasketService.Contracts.Requests;
using FluentValidation;

namespace BasketService.Application.Validation
{
    public sealed class ChangeQuantityRequestValidator : AbstractValidator<ChangeQuantityRequest>
    {
        public ChangeQuantityRequestValidator()
        {
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
