using BasketService.Contracts.Requests;
using FluentValidation;

namespace BasketService.Application.Validation
{
    public sealed class UpsertBasketItemsRequestValidator : AbstractValidator<UpsertBasketItemsRequest>
    {
        public UpsertBasketItemsRequestValidator()
        {
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(new BasketItemDtoValidator());
        }
    }
}
