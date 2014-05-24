using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.TagSoup;

namespace Tuulbox.Regexes
{
    sealed class ParseException : Exception
    {
        public int Index { get; private set; }
        public object HtmlMessage { get; private set; }
        public ParseException(int index, object htmlMessage)
            : base(Tag.ToString(htmlMessage))
        {
            Index = index;
            HtmlMessage = htmlMessage;
        }
    }
}
