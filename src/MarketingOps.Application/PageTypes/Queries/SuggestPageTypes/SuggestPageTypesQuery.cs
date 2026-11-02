using MediatR;

namespace MarketingOps.Application.PageTypes.Queries.SuggestPageTypes;

public sealed record SuggestPageTypesQuery(Guid SiteId, PageTypeSuggestionSource Source) : IRequest<PageTypeSuggestionsResponse>;
