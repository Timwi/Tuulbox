using System;
using RT.TagSoup;

namespace Tuulbox
{
    static class Helpers
    {
        public static object TextWithAccessKey(string text, string accessKey)
        {
            object textHtml = text;
            var pos = text.IndexOf(accessKey, StringComparison.OrdinalIgnoreCase);
            if (pos == -1)
                return textHtml;
            return new object[] { text.Substring(0, pos), new KBD { class_ = "accesskey" }._(text.Substring(pos, accessKey.Length)), text.Substring(pos + accessKey.Length) };
        }

        public static object LabelWithAccessKey(string text, string accessKey, string forId)
        {
            return new LABEL { for_ = forId, accesskey = accessKey }._(TextWithAccessKey(text, accessKey));
        }
    }
}
