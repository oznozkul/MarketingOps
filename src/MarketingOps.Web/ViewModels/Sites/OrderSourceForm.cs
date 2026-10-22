using MarketingOps.Application.Sites.Commands.UpdateOrderSource;
using MarketingOps.Application.Sites.Queries.GetSiteById;

namespace MarketingOps.Web.ViewModels.Sites;

public sealed class OrderSourceForm
{
    public string? ConnectionString { get; set; }
    public string? Query { get; set; }
    public bool IsConfigured { get; set; }

    public static OrderSourceForm From(OrderSourceResponse source) => new()
    {
        Query = source.Query,
        IsConfigured = source.IsConfigured
    };

    public UpdateOrderSourceCommand ToCommand(Guid siteId) => new(siteId, ConnectionString, Query);
}
