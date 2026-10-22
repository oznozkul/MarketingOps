using System.Text;
using System.Text.RegularExpressions;

namespace MarketingOps.Domain.Sites;

/// <summary>
/// URL path'i için glob deseni. <c>*</c> tek bir path segmentiyle, <c>**</c> sıfır ya da daha fazla segmentle eşleşir.
/// Desen query string'i değil yalnızca path'i hedefler.
/// </summary>
public static class GlobPattern
{
    public static Regex ToRegex(string pattern)
    {
        var path = pattern.Trim();
        if (!path.StartsWith('/')) path = "/" + path;

        var sb = new StringBuilder("^");
        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            if (c == '*' && i + 1 < path.Length && path[i + 1] == '*')
            {
                // "/**" sonda ya da ortada: alt klasörlerin tamamını (boş dahil) kapsar.
                sb.Append(".*");
                i++;
            }
            else if (c == '*')
            {
                sb.Append("[^/]+");
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }
        sb.Append("/?$");
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }
}
