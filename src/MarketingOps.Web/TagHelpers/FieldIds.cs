namespace MarketingOps.Web.TagHelpers;

/// <summary>Alan adından (ör. "Conditions[0].Value") geçerli bir HTML kimliği üretir.</summary>
public static class FieldIds
{
    public static string Id(string field) => field.Replace('[', '_').Replace(']', '_').Replace('.', '_');

    public static string ErrorId(string field) => Id(field) + "-error";
}
