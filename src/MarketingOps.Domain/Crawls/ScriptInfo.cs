namespace MarketingOps.Domain.Crawls;

public sealed record ScriptInfo(string? Src, bool Async, bool Defer, bool InHead);
