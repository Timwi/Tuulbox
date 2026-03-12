using RT.Servers;
using RT.TagSoup;
using RT.Util;
using Tuulbox.Regexes;

namespace Tuulbox.Tools;

internal sealed class RegexesTool : ITuul
{
    bool ITuul.Enabled => true;
    bool ITuul.Listed => true;
    string ITuul.Name => "Regular expressions";
    string ITuul.UrlName => "regexes";
    string ITuul.Keywords => "regular expressions regexes regexps regex match text pattern";
    string ITuul.Description => "Explains the meaning of each element in a regular expression.";
    string ITuul.Js => Resources.RegexesJs;
    string ITuul.Css => @"
#regex-show { color: black; font-size: 250%; white-space: pre-wrap; word-break: break-all; }
#regex-show:hover .node { color: #ddd; }
#regex-show:hover .node.innerhover:hover { color: #000; }
#regex-show:hover .node.innerhover:hover .node { color: #68a; }

#regex-explain {
    position: absolute;
    display: none;
    padding: .5em 1.2em;
    background-image: linear-gradient(bottom, #ace 0%, #def 100%);
    background-image: -o-linear-gradient(bottom, #ace 0%, #def 100%);
    background-image: -moz-linear-gradient(bottom, #ace 0%, #def 100%);
    background-image: -webkit-linear-gradient(bottom, #ace 0%, #def 100%);
    background-image: -ms-linear-gradient(bottom, #ace 0%, #def 100%);
    border: 2px solid #666;
    border-radius: 1em;
    color: black;
    box-shadow: 3px 2px 5px rgba(0, 0, 0, .7);
    text-shadow: 0 0 5px white;
    max-width: 25em;
}

#regex-explain h3, #regex-explain p { margin: .7em 0; }
";

    object ITuul.Handle(TuulboxModule module, HttpRequest req)
    {
        string regex = null;
        object html = null;

        if (req.Method == HttpMethod.Post)
        {
            regex = req.Post["regex"].Value.Replace("\r", "");
            if (regex != null)
            {
                try
                {
                    var match = RegexesUtil.Grammar.MatchExact(regex.ToCharArray());
                    html = match != null
                        ? Ut.NewArray<object>(
                            new P("Here is a breakdown of your regular expression. Hover the mouse over any item to get information about it."),
                            new DIV(new SPAN { id = "regex-show", class_ = "input" }._(match.Result.Html)))
                        : ([new H2("Parse error"), new P("Your regular expression couldn’t be parsed. If you think it’s a valid regular expression, please send it to us so we can fix our parser.")]);
                }
                catch (ParseException pe)
                {
                    html = new DIV { class_ = "error" }._(
                        new DIV { id = "regex-show", class_ = "input" }._(
                            new SPAN { class_ = "good" }._(regex.Substring(0, pe.StartIndex)),
                            new SPAN { class_ = "indicator" },
                            new SPAN { class_ = "bad" }._(regex.Substring(pe.StartIndex, pe.EndIndex - pe.StartIndex)),
                            pe.EndIndex != pe.StartIndex ? new SPAN { class_ = "indicator" } : null,
                            new SPAN { class_ = "rest" }._(regex.Substring(pe.EndIndex))
                        ),
                        pe.HtmlMessage
                    );
                }
            }
        }

        return new FORM { method = method.post, action = req.Url.ToFull() }._(
            html,
            new TABLE { class_ = "layout" }._(
                new TR(
                    new TD(
                        new H3(Helpers.LabelWithAccessKey("Regular expression", "r", "regex_regex")),
                        new P("Type any regular expression to get information about it."),
                        new DIV(new TEXTAREA { name = "regex", id = "regex_regex", accesskey = "," }._(regex))
                    )
                )
            ),
            new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Go for it", "g")))
        );
    }
}
