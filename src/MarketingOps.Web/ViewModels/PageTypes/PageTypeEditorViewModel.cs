namespace MarketingOps.Web.ViewModels.PageTypes;

/// <summary><see cref="PageTypeId"/> null ise yeni sayfa tipi eklenir.</summary>
public sealed record PageTypeEditorViewModel(Guid? PageTypeId, PageTypeForm Form);
