using System;
using RT.TagSoup;

namespace Tuulbox
{
    static class Helpers
    {
        public static object LabelWithAccessKey(string line, string accessKey, string forId, string tooltip = null)
        {
            object lineHtml = line;
            var pos = line.IndexOf(accessKey, StringComparison.OrdinalIgnoreCase);
            if (pos != -1)
                lineHtml = new object[] { line.Substring(0, pos), new U(line.Substring(pos, accessKey.Length)), line.Substring(pos + accessKey.Length) };
            return new LABEL { for_ = forId, title = tooltip, accesskey = accessKey }._(lineHtml);
        }
    }
}
