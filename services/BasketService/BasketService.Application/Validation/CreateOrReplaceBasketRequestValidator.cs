using BasketService.Contracts.Requests;
using FluentValidation;

namespace BasketService.Application.Validation
{
    public sealed class CreateOrReplaceBasketRequestValidator : AbstractValidator<CreateOrReplaceBasketRequest>
    {
        public CreateOrReplaceBasketRequestValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(new BasketItemDtoValidator());
        }
    }
}
