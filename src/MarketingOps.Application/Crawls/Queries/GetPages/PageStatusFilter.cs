namespace MarketingOps.Application.Crawls.Queries.GetPages;

public enum PageStatusFilter
{
    Success,
    Redirect,
    ClientError,
    ServerError,
    NoResponse
}
