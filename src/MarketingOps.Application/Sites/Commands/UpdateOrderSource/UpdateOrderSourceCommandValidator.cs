using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Commands.UpdateOrderSource;

public sealed class UpdateOrderSourceCommandValidator : AbstractValidator<UpdateOrderSourceCommand>
{
    public UpdateOrderSourceCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.ConnectionString).MaximumLength(4000).WithMessage("Bağlantı cümlesi çok uzun.");
        RuleFor(x => x.Query).MaximumLength(OrderSourceSettings.QueryMaxLength).WithMessage("Sipariş sorgusu çok uzun.");
    }
}
