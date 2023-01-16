using System.Drawing.Text;
using System.Linq;

namespace DanmakuPlayer.Resources;

public static class ConstantStrings
{
    public const string AuthorUri = "https://github.com/Poker-sang";

    public const string RepositoryUri = AuthorUri + "/DanmakuPlayer";

    public const string LicenseUri = RepositoryUri + "/blob/master/LICENSE";

    public const string MailUri = "mailto:poker_sang@outlook.com";

    public const string QqUri = "http://wpa.qq.com/msgrd?v=3&uin=2639914082&site=qq&menu=yes";


    public static string[] FontFamilies
    {
        get
        {
            using var collection = new InstalledFontCollection();
            return collection.Families.Select(t => t.Name).ToArray();
        }
    }
}
