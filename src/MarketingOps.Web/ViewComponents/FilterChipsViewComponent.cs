using MarketingOps.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.ViewComponents;

/// <summary>Aktif filtreleri chip olarak gösterir; her chip tek tıkla kaldırılır.</summary>
public class FilterChipsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<ChipViewModel> chips, string clearUrl, string target = "#results") =>
        View(new FilterChipsViewModel(chips, clearUrl, target));
}
