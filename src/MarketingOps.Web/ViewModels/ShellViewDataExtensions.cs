using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MarketingOps.Web.ViewModels;

public static class ShellViewDataExtensions
{
    public const string Key = "Shell";

    public static ShellViewModel? Shell(this ViewDataDictionary viewData) => viewData[Key] as ShellViewModel;
}
