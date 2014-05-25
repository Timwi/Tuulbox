using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.TagSoup;

namespace Tuulbox.Regexes
{
    sealed class ParseException : Exception
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
}
