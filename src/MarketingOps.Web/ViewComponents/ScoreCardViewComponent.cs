using MarketingOps.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.ViewComponents;

/// <summary>Skor kartı: değer, önceki değere göre değişim ve "Bu skor nasıl hesaplandı?" paneli.</summary>
public class ScoreCardViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ScoreCardViewModel model) => View(model);
}
