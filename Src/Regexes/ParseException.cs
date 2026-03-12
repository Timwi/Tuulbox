using System;
using RT.TagSoup;

namespace Tuulbox.Regexes;

internal sealed class ParseException(int startIndex, int endIndex, object htmlMessage) : Exception(Tag.ToString(htmlMessage))
{
    public int StartIndex { get; private set; } = startIndex;
    public int EndIndex { get; private set; } = endIndex;
    public object HtmlMessage { get; private set; } = htmlMessage;
}
