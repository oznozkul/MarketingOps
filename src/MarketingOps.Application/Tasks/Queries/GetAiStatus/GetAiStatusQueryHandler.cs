using MarketingOps.Application.Common.Interfaces;
using MediatR;

namespace MarketingOps.Application.Tasks.Queries.GetAiStatus;

public sealed class GetAiStatusQueryHandler(IAiSuggestionService? ai = null) : IRequestHandler<GetAiStatusQuery, AiStatusResponse?>
{
    public Task<AiStatusResponse?> Handle(GetAiStatusQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(ai is null ? null : new AiStatusResponse(ai.ProviderName, ai.ModelName, ai.SendsDataExternally));
}
