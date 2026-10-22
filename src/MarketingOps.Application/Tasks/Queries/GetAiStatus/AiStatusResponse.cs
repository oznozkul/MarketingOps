namespace MarketingOps.Application.Tasks.Queries.GetAiStatus;

public sealed record AiStatusResponse(string Provider, string Model, bool SendsDataExternally);
