namespace MarketingOps.Application.Tracking;

/// <summary>Bir sayfanın denetim gözlemi. Yüklenemeyen sayfa bulguya yazılmaz.</summary>
public sealed record PageObservation(string Url, bool Loaded, bool ConsentClicked, IReadOnlyList<ObservedRequest> Requests);
