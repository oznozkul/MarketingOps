using MarketingOps.Application.Crawls.Queries.GetPages;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Sites;
using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.Scores;
using MarketingOps.Domain.Tracking;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Web.Infrastructure;

/// <summary>Enum değerlerinin arayüzdeki Türkçe karşılıkları. Tek kaynak burasıdır.</summary>
public static class DisplayText
{
    public static string Of(Severity v) => v switch
    {
        Severity.Critical => "Kritik",
        Severity.High => "Yüksek",
        Severity.Medium => "Orta",
        _ => "Düşük"
    };

    public static string Of(FindingStatus v) => v switch
    {
        FindingStatus.Open => "Açık",
        FindingStatus.Reopened => "Tekrar açıldı",
        FindingStatus.Resolved => "Çözüldü",
        FindingStatus.FalsePositive => "Yanlış alarm",
        _ => "Ertelendi"
    };

    public static string Of(FindingCategory v) => v switch
    {
        FindingCategory.Tracking => "Tracking",
        FindingCategory.TechnicalSeo => "Teknik SEO",
        FindingCategory.Content => "İçerik",
        FindingCategory.Performance => "Performans",
        _ => "Erişilebilirlik ve mobil"
    };

    public static string Of(WorkTaskStatus v) => v switch
    {
        WorkTaskStatus.Todo => "Yapılacak",
        WorkTaskStatus.InProgress => "Devam ediyor",
        WorkTaskStatus.AwaitingVerification => "Doğrulama bekliyor",
        _ => "Kapandı"
    };

    public static string Of(CrawlStatus v) => v switch
    {
        CrawlStatus.Queued => "Kuyrukta",
        CrawlStatus.Running => "Çalışıyor",
        CrawlStatus.Completed => "Tamamlandı",
        CrawlStatus.PartiallyCompleted => "Kısmen tamamlandı",
        CrawlStatus.Failed => "Hata",
        _ => "İptal edildi"
    };

    public static string Of(ConnectorStatus v) => v switch
    {
        ConnectorStatus.Connected => "Bağlı",
        ConnectorStatus.NotConnected => "Bağlı değil",
        ConnectorStatus.PendingApproval => "Onay bekleniyor",
        ConnectorStatus.Expired => "Yetki süresi doldu",
        _ => "Hata"
    };

    public static string Of(TagAuditStatus v) => v switch
    {
        TagAuditStatus.Ok => "Doğru",
        TagAuditStatus.Missing => "Eksik",
        TagAuditStatus.Duplicate => "İki kez yüklü",
        TagAuditStatus.WrongId => "Yanlış ID",
        _ => "Onay öncesi tetikleniyor"
    };

    public static string Of(TagVendor v) => v switch
    {
        TagVendor.Gtm => "GTM",
        TagVendor.Ga4 => "GA4",
        TagVendor.GoogleAds => "Google Ads",
        _ => "Meta Pixel"
    };

    public static string Of(AuditStatus v) => v switch
    {
        AuditStatus.Running => "Çalışıyor",
        AuditStatus.Completed => "Tamamlandı",
        _ => "Başarısız"
    };

    public static string Of(CrawlSchedule v) => v switch
    {
        CrawlSchedule.Daily => "Günde bir",
        CrawlSchedule.Every12Hours => "12 saatte bir",
        _ => "Haftada bir"
    };

    public static string Of(CrawlTrigger v) => v == CrawlTrigger.Schedule ? "Zamanlanmış" : "Manuel";

    public static string Of(RenderDevice v) => v switch
    {
        RenderDevice.Desktop => "Masaüstü",
        RenderDevice.Tablet => "Tablet",
        _ => "Mobil"
    };

    public static string Of(CrawlStage v) => v switch
    {
        CrawlStage.Discovery => "Keşif",
        CrawlStage.Download => "İndirme",
        CrawlStage.Render => "Render",
        CrawlStage.RuleEvaluation => "Kural değerlendirme",
        _ => "Connector senkronu"
    };

    public static string Of(SkipReason v) => v switch
    {
        SkipReason.RobotsTxt => "robots.txt engelli",
        SkipReason.ExcludedPath => "Hariç tutulan path",
        SkipReason.MaxPagesReached => "Sayfa limiti doldu",
        _ => "Dış alan adı"
    };

    public static string Of(PageStatusFilter v) => v switch
    {
        PageStatusFilter.Success => "2xx başarılı",
        PageStatusFilter.Redirect => "3xx yönlendirme",
        PageStatusFilter.ClientError => "4xx istemci hatası",
        PageStatusFilter.ServerError => "5xx sunucu hatası",
        _ => "Yanıt yok"
    };

    public static string Of(MatchMode v) => v == MatchMode.All ? "Tüm koşullar (VE)" : "Herhangi biri (VEYA)";

    public static string Of(MatchConditionType v) => v switch
    {
        MatchConditionType.UrlGlob => "URL deseni",
        MatchConditionType.UrlRegex => "URL regex",
        _ => "Schema @type"
    };

    public static string Of(ConnectorType v) => v switch
    {
        ConnectorType.SearchConsole => "Search Console",
        ConnectorType.Ga4 => "GA4",
        ConnectorType.Gtm => "Google Tag Manager",
        ConnectorType.GoogleAds => "Google Ads",
        ConnectorType.Meta => "Meta Pixel / CAPI",
        ConnectorType.MerchantCenter => "Merchant Center",
        ConnectorType.OrderSource => "Sipariş kaynağı",
        _ => "Görev hedefi"
    };

    public static string Of(DeviceStrategy v) => v == DeviceStrategy.Mobile ? "Mobil" : "Masaüstü";

    public static string Of(VitalsSource v) => v switch
    {
        VitalsSource.UrlField => "Gerçek kullanıcı (sayfa)",
        VitalsSource.OriginField => "Gerçek kullanıcı (site geneli)",
        _ => "Laboratuvar ölçümü"
    };

    public static string Of(Team v) => v switch
    {
        Team.Frontend => "Frontend",
        Team.Backend => "Backend",
        Team.Seo => "SEO",
        _ => "İçerik"
    };

    public static string Of(SuggestionKind v) => v switch
    {
        SuggestionKind.Text => "Metin önerisi",
        SuggestionKind.Code => "Kod önerisi",
        _ => "Yapılandırma adımları"
    };

    public static string Of(VerificationType v) => v == VerificationType.Automatic ? "Otomatik · sonraki tarama" : "Ekip onayı";

    public static string Of(ScoreCategory v) => v switch
    {
        ScoreCategory.Tracking => "Tracking Sağlığı",
        ScoreCategory.Seo => "SEO Sağlığı",
        ScoreCategory.Performance => "Performans",
        _ => "Erişilebilirlik ve Mobil Sağlığı"
    };

    public static string Of(NotificationEventType v) => v switch
    {
        NotificationEventType.NewCriticalFinding => "Yeni kritik bulgu",
        NotificationEventType.FindingReopened => "Tekrar açılan bulgu",
        NotificationEventType.PurchaseGap => "Purchase farkı eşiği aştı",
        NotificationEventType.ScoreDropped => "Skor düştü",
        NotificationEventType.CrawlFailed => "Tarama başarısız",
        _ => "Bağlantı yetkisi doldu"
    };

    public static string Of(NotificationChannelKind v) => v switch
    {
        NotificationChannelKind.Email => "E-posta",
        NotificationChannelKind.Teams => "Teams",
        NotificationChannelKind.Slack => "Slack",
        _ => "Panel"
    };

    public static string Of(ReportPeriod v) => v switch
    {
        ReportPeriod.Daily => "Günlük",
        ReportPeriod.Weekly => "Haftalık",
        _ => "Aylık"
    };

    public static string Of(SiteSetupStep v) => v switch
    {
        SiteSetupStep.SearchConsole => "Search Console ve sahiplik",
        SiteSetupStep.Connectors => "Diğer bağlantılar",
        SiteSetupStep.PageTypes => "Sayfa tipleri",
        SiteSetupStep.CrawlSettings => "Tarama ayarları",
        _ => "İlk tarama"
    };

    public static string Of(Enum value) => value switch
    {
        Team v => Of(v),
        DeviceStrategy v => Of(v),
        VitalsSource v => Of(v),
        SuggestionKind v => Of(v),
        VerificationType v => Of(v),
        Severity v => Of(v),
        FindingStatus v => Of(v),
        FindingCategory v => Of(v),
        WorkTaskStatus v => Of(v),
        CrawlStatus v => Of(v),
        ConnectorStatus v => Of(v),
        TagAuditStatus v => Of(v),
        TagVendor v => Of(v),
        AuditStatus v => Of(v),
        CrawlSchedule v => Of(v),
        MatchMode v => Of(v),
        MatchConditionType v => Of(v),
        ConnectorType v => Of(v),
        CrawlTrigger v => Of(v),
        CrawlStage v => Of(v),
        RenderDevice v => Of(v),
        SkipReason v => Of(v),
        PageStatusFilter v => Of(v),
        ScoreCategory v => Of(v),
        NotificationEventType v => Of(v),
        NotificationChannelKind v => Of(v),
        ReportPeriod v => Of(v),
        SiteSetupStep v => Of(v),
        _ => value.ToString()
    };
}
