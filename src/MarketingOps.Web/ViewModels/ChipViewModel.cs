namespace MarketingOps.Web.ViewModels;

/// <summary>Aktif filtre chip'i: grup adı ("Önem"), değer ("Kritik") ve kaldırma bağlantısı.</summary>
public record ChipViewModel(string Group, string Label, string RemoveUrl);
