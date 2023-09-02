using System;
using RT.TagSoup;

namespace Tuulbox.Regexes;

internal sealed class ParseException : Exception
{
    public int StartIndex { get; private set; }
    public int EndIndex { get; private set; }
    public object HtmlMessage { get; private set; }
    public ParseException(int startIndex, int endIndex, object htmlMessage)
        : base(Tag.ToString(htmlMessage))
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
        HtmlMessage = htmlMessage;
    }
}
