using System;
using RT.TagSoup;

namespace Tuulbox;

internal static class Helpers
{
    public static object TextWithAccessKey(string text, string accessKey)
    {
        object textHtml = text;
        var pos = text.IndexOf(accessKey, StringComparison.OrdinalIgnoreCase);
        if (pos == -1)
            return textHtml;
        return new object[] { text.Substring(0, pos), new KBD { class_ = "accesskey" }._(text.Substring(pos, accessKey.Length)), text.Substring(pos + accessKey.Length) };
    }

    public static object LabelWithAccessKey(string text, string accessKey, string forId) => new LABEL { for_ = forId, accesskey = accessKey }._(TextWithAccessKey(text, accessKey));

    public static string ElideFront(this string str, int maxChars)
    {
        if (str.Length > maxChars)
            return "..." + str.Substring(str.Length - (maxChars - 3));
        return str;
    }

    public static string ElideBack(this string str, int maxChars)
    {
        if (str.Length > maxChars)
            return str.Substring(0, maxChars - 3) + "...";
        return str;
    }
}
