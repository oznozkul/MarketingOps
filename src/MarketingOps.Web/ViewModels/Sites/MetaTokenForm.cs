using MarketingOps.Application.Connectors.Commands.ConnectMeta;

namespace MarketingOps.Web.ViewModels.Sites;

public sealed class MetaTokenForm
{
    public string? AccessToken { get; set; }
    public bool Remove { get; set; }
    public bool IsConnected { get; set; }

    public ConnectMetaCommand ToCommand(Guid siteId) => new(siteId, AccessToken, Remove);
}
