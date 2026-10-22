namespace MarketingOps.Web.ViewModels;

/// <summary>Ortak modal kabuğu: kimlik, başlık ve içerik partial'ı.</summary>
public sealed record ModalViewModel(string Id, string Title, string PartialName, object PartialModel);
