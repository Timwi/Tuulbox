using System.Text.RegularExpressions;
using RT.Generexes;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using Tuulbox.Regexes;

namespace Tuulbox.Tools
{
    sealed class RegexesTool : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return true; } }
        string ITuul.Name { get { return "Regular expressions"; } }
        string ITuul.UrlName { get { return "regexes"; } }
        string ITuul.Keywords { get { return "regular expressions regexes regexps regex match text pattern"; } }
        string ITuul.Description { get { return "Explains the meaning of each element in a regular expression."; } }
        string ITuul.Js { get { return _js ?? (_js = generateJs()); } }
        string ITuul.Css
        {
            get
            {
                return @"
#regex-show { color: black; font-size: 250%; white-space: pre; }
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

.regex-error {
    background: #fff6ee;
    border: 1px solid #f82;
    padding: .5em 1em;
    position: relative;
}
.regex-error:before {
    position: absolute;
    right: 0;
    top: 0;
    background: #f82;
    content: 'Error';
    padding: .1em .5em;
    color: white;
    font-weight: bold;
}
.regex-error #regex-show { margin-bottom: .7em; position: relative; }
.regex-error .regex-good { color: #082; }
.regex-error .regex-bad { color: #a24; }
.regex-error .regex-rest { color: #ddd; }
.regex-error .regex-indicator { color: #a24; position: absolute; font-size: 70%; }
.regex-error .regex-indicator:before { content: '^'; position: relative; left: -50%; top: 1.4em; }
";
            }
        }

        private static string _js;
        private static string generateJs()
        {
            return @"
$(function()
{
    var $curTooltip = null;

    var htmlEscape = function(str)
    {
        return str.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('""', '&quot;').replace(""'"", '&#39;');
    };

    var explains = [
        [ 'then', '<h3>Literal</h3><p>Matches the specified text.</p>' ],
        [ 'literal', function(elem) { return '<h3>Literal</h3><p>Matches ' + htmlEscape(elem.data('text')) + '.' + (elem.data('isqe') ? '<p>The <code>\\Q</code>...<code>\\E</code> operator is not supported by .NET.' : ''); } ],
        [ 'escapedliteral', function(elem) { return '<h3>Escaped literal</h3><p>Matches ' + elem.data('char') + '.</p>'; } ],
        [ 'or', '<h3>Or</h3><p>Matches any one of the several subexpressions.</p><p class=""extra"">Also known as “alternation”.</p>' ],
        [ 'any', '<h3>Any character</h3><p>In single-line mode, matches a single character. Without single-line mode, matches a single character except newlines.</p>' ],
        [ 'start', '<h3>Beginning of line or string</h3><p>In multi-line mode, matches at the beginning of a line. Without multi-line mode, matches only at the beginning of the entire input string.</p>' ],
        [ 'end', '<h3>End of line or string</h3><p>In multi-line mode, matches at the end of a line. Without multi-line mode, matches either at the end of the entire input string, or before a <code>\\n</code> (newline) character at the end of the entire input string.</p>' ],
        [ 'namedbackref', '<h3>Named backreference</h3><p>Matches only the same text that was previously matched by the named capturing parenthesis with the same name.' ],

        [ 'characterclass', function(elem)
        {
            var info = elem.data('info');
            var negated = elem.data('negated');
            var str = negated
                ? '<h3>Negated character class</h3><p>Matches a single character that is not any of the following:</p><ul>'
                : '<h3>Character class</h3><p>Matches any of the following characters:</p><ul>';
            for (var i = 0; i < info.length; i++)
                str += '<li>' + $('<div>').text(info[i]).html() + '</li>';
            return str + '</ul>';
        }],

        [ 'escapecode', function(elem)
        {
            switch (elem.data('code'))
            {
                case 'BeginningOfString': return '<h3>Beginning of string</h3><p>Matches only at the beginning of the string, irrespective of the multi-line setting.</p>';
                case 'WordBoundary': return '<h3>Word boundary</h3><p>Matches at places where the string changes from a word character to a non-word character or vice versa, or at the beginning or end of the string if it straddles a word character.</p><p>For example, in the string:</p><blockquote><code>""he (or she)""</code></blockquote><p>there are six word boundaries, here marked with vertical lines:</p><blockquote><code>""</code>|<code>he</code>|<code> (</code>|<code>or</code>|<code> </code>|<code>she</code>|<code>)""</code></blockquote>';
                case 'NonWordBoundary': return '<h3>Non-word boundary</h3><p>Matches at places where there is not a word boundary. See <code>\\b</code> for an example.</p>';
                case 'Digit': return '<h3>Digit</h3><p>Matches any digit character, including digits from other scripts (such as the Hindi numbers).</p>';
                case 'NonDigit': return '<h3>Non-digit</h3><p>Matches any character that is not a digit character.</p>';
                case 'SpaceCharacter': return '<h3>Space character</h3><p>Matches any space character, including the tab, the em space, etc.</p>';
                case 'NonSpaceCharacter': return '<h3>Non-space character</h3><p>Matches any character that is not a space character (which includes the tab, the em space, etc.).</p>';
                case 'WordCharacter': return '<h3>Word character</h3><p>Matches any word character, which includes letters, digits, and connector punctuation (e.g. the underscore, U+005F).</p>';
                case 'NonWordCharacter': return '<h3>Non-word character</h3><p>Matches any character that is not a word character (which includes letters, digits, and connector punctuation such as the underscore, U+005F).</p>';
                case 'EndOfStringAlmost': return '<h3>End of string</h3><p>Matches either at the end of the entire input string, or before a <code>\\n</code> (newline) character at the end of the entire input string, irrespective of the multi-line setting.</p><p>To match only the end of the string, use <code>\\Z</code> (upper-case Z).</p>';
                case 'EndOfStringReally': return '<h3>End of string only</h3><p>Matches only at the end of the entire input string, irrespective of the multi-line setting.</p><p>To also match before a <code>\\n</code> (newline) character at the end of the input string, use <code>\\z</code> (lower-case z).</p>';
            }
        }],

        [ 'parenthesis', function(elem)
        {
            function flagList(flags)
            {
                var list = [];
                if (flags & 1) list.push('<li><b>Ignore case</b>');
                if (flags & 2) list.push('<li><b>Multi-line mode</b> — changes the meaning of <code>^</code> and <code>$</code>.');
                if (flags & 4) list.push('<li><b>Explicit capture</b> — when on, plain parentheses do not capture their matches automatically. This option is only supported by .NET.');
                if (flags & 8) list.push('<li><b>Single-line mode</b> — changes the meaning of <code>.</code>.');
                if (flags & 16) list.push('<li><b>Ignore whitespace</b> — when on, ignores whitespace and comments initiated by <code>#</code> until the end of the line.');
                return '<ul>' + list.join('') + '</ul>';
            }
            switch (elem.data('type'))
            {
                case 'Capturing': return '<h3>Capture</h3><p>Captures (remembers) the subexpression. The string matched by the subexpression is typically stored in a variable.<p>To group expressions without capturing their matches, use <code>(?:...)</code>.';
                case 'NamedCapturing': return '<h3>Named capture</h3><p>Captures (remembers) the subexpression and gives the string matched by the subexpression the name “' + elem.data('groupname') + '”.<p>To group expressions without capturing their matches, use <code>(?:...)</code>.';
                case 'Grouping': return '<h3>Group</h3><p>Groups several literals and operators into a subexpression, typically to apply an operator such as <code>*</code> on the entire subexpression.<p>To capture the match of a subexpression into a variable, omit the <code>?:</code>.';
                case 'PositiveLookAhead': return '<h3>Positive zero-width look-ahead assertion</h3><p>Checks if the text following the current position matches the subexpression, but doesn’t consume any of it; if the subexpression matches, returns a zero-width match.<p>To check if the following text does <em>not</em> match the subexpression, use <code>(?!...)</code>.<p>To check if the <em>earlier</em> text matches the subexpression (look-<em>behind</em>), use <code>(?&lt;=...)</code>.';
                case 'NegativeLookAhead': return '<h3>Negative zero-width look-ahead assertion</h3><p>Checks if the text following the current position <em>does not</em> match the subexpression. If the subexpression does not match, returns a zero-width match.<p>To check if the following text <em>does</em> match the subexpression, use <code>(?=...)</code>.<p>To check if the <em>earlier</em> text does not match the subexpression (look-<em>behind</em>), use <code>(?&lt;!...)</code>.';
                case 'PositiveLookBehind': return '<h3>Positive zero-width look-behind assertion</h3><p>Checks if the text before the current position (i.e. the text matched by the regular expression so far, and possibly more text before that) matches the subexpression. If the subexpression matches, returns a zero-width match.<p>To check if the earlier text does <em>not</em> match the subexpression, use <code>(?&lt;!...)</code>.<p>To check if the <em>following</em> text matches the subexpression (look-<em>ahead</em>), use <code>(?=...)</code>.';
                case 'NegativeLookBehind': return '<h3>Negative zero-width look-behind assertion</h3><p>Checks if the text before the current position (i.e. the text matched by the regular expression so far, and possibly more text before that) <em>does not</em> match the subexpression. If the subexpression does not match, returns a zero-width match.<p>To check if the earlier text <em>does</em> match the subexpression, use <code>(?&lt;=...)</code>.<p>To check if the <em>following</em> text does not match the subexpression (look-<em>ahead</em>), use <code>(?!...)</code>.';
                case 'Atomic': return '<h3>Atomic match</h3><p>Allows the subexpression to use only the first match it finds. The subexpression can use backtracking to find that first match, but once it has found it, it must stick to it; if the rest of the regular expression (after the atomic operator) then does not match, the subexpression is not allowed to backtrack.';

                case 'Flags':
                    var enable = elem.data('enable');
                    var disable = elem.data('disable');
                    if (enable)
                    {
                        return '<h3>Enable/disable options</h3><p><b>Enables</b> the following options:' + flagList(enable) +
                            (disable ? '<p>and <b>disables</b> the following options:' + flagList(disable) : '');
                    }
                    else
                    {
                        return '<h3>Enable/disable options</h3><p><b>Disables</b> the following options:' + flagList(disable);
                    }
            }
        }],

        [ 'repeater', function(elem)
        {
            var html;
            if (elem.data('type') == 'Optional')
            {
                html = '<h3>Optional</h3>';
                html += '<p>Makes the subexpression optional; in other words, it may match once or not at all.';
                html += '<p>This is equivalent to <code>{0,1}' + (elem.data('greediness') == 'Greedy' ? '' : '?') + '</code>.';
                if (elem.data('greediness') == 'Nongreedy')
                    html += '<p>The operator is <b>non-greedy</b>: it matches the empty string first, and if no match is found for the rest of the regular expression, matches the subexpression and then tries again. To use the greedy variant, use <code>?</code> instead of <code>??</code>.';
                else
                    html += '<p>The operator is <b>greedy</b>: it matches the subexpression first, and if no match is found for the rest of the regular expression, matches the empty string and then tries again. To use the non-greedy variant, use <code>??</code>.';
            }
            else
            {
                html = '<h3>Repeater</h3>';
                var op = '';
                switch (elem.data('type'))
                {
                    case 'Star': op = '*'; html += '<p>Matches the subexpression any number of times, including zero times.<p>To exclude zero times, use <code>+</code> instead of <code>*</code>.'; break;
                    case 'Plus': op = '+'; html += '<p>Matches the subexpression any number of times, but at least once.<p>To include zero times, use <code>*</code> instead of <code>+</code>.'; break;
                    case 'Min': op = '{' + elem.data('min') + ',}'; html += '<p>Matches the subexpression any number of times, but at least ' + elem.data('min') + ' times.'; break;
                    case 'Max': op = '{,' + elem.data('max') + '}'; html += '<p>Matches the subexpression up to ' + elem.data('max') + ' times (including possibly zero times).<p>To exclude zero times, use <code>{1,' + elem.data('max') + '}</code> instead. To include infinite number of times, use <code>*</code> instead.'; break;
                    case 'MinMax': op = '{' + elem.data('min') + ',' + elem.data('max') + '}'; html += '<p>Matches the subexpression between ' + elem.data('min') + ' and ' + elem.data('max') + ' times.<p>To include infinite number of times, use <code>{' + elem.data('min') + ',}</code> instead.'; break;
                    case 'Specific': op = '{' + elem.data('min') + '}'; html += '<p>Matches the subexpression exactly ' + elem.data('min') + ' times.<p>To match the subexpression a variable number of times, use (for instance) <code>{5,10}</code> for 5 to 10 times.'; break;
                }
                if (elem.data('greediness') == 'Nongreedy')
                    html += '<p>The operator is <b>non-greedy</b>: it matches the subexpression the <em>minimum</em> number of times first, and if no match is found for the rest of the regular expression, matches the subexpression <em>more</em> times and then tries again. To use the greedy variant, remove the <code>?</code>.';
                else if (elem.data('greediness') == 'Atomic')
                    html += '<p>The operator is <b>atomic</b>: it matches only as much as it can and does not backtrack. To use the backtracking variant, remove the extra <code>+</code>.<p>Not supported by .NET. Use <code>(?>...' + op + ')</code> instead.';
                else
                    html += '<p>The operator is <b>greedy</b>: it matches the subexpression the <em>maximum</em> number of times first, and if no match is found for the rest of the regular expression, matches the subexpression <em>fewer</em> times and tries again. To use the non-greedy variant, add a <code>?</code> after the operator.';
            }
            return html;
        }]
    ];

    var processNodeHover = function()
    {
        var elem = $(':hover').last();
        var ex = $('#regex-explain');
        if (!ex.length)
            ex = $('<div id=""regex-explain""></div>').appendTo(document.body);
        if (elem.hasClass('node'))
        {
            $('.innerhover').removeClass('innerhover');
            elem.addClass('innerhover');
            var html;
            for (var i = 0; i < explains.length && !html; i++)
                if (elem.hasClass('node') && elem.hasClass(explains[i][0]))
                    html = typeof explains[i][1] === 'function' ? explains[i][1](elem) : explains[i][1];
            ex.html(html || '<p>We don’t have any explanation for this element yet.');
            ex.show().position({ my: 'left top', at: 'left bottom', of: elem });
        }
        else
            ex.hide();
    };

    $('.node').hover(processNodeHover, function() { window.setTimeout(processNodeHover, 1); });
});
";
        }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            string regex = null, input = null;
            bool single = true, multi = false, ignoreCase = false, ignoreWhitespace = false;
            bool invalid = false;
            object html = null;

            if (req.Method == HttpMethod.Post)
            {
                regex = req.Post["regex"].Value.Replace("\r", "");
                input = req.Post["input"].Value;
                single = req.Post["s"].Value == "1";
                multi = req.Post["m"].Value == "1";
                ignoreCase = req.Post["i"].Value == "1";
                ignoreWhitespace = req.Post["x"].Value == "1";

                if (regex != null)
                {
                    try
                    {
                        var match = RegexesUtil.Grammar.MatchExact(regex.ToCharArray());
                        if (match != null)
                            html = Ut.NewArray<object>(
                                new P("Here is a breakdown of your regular expression. Hover the mouse over any item to get information about it."),
                                new DIV(new SPAN { id = "regex-show" }._(match.Result.ToHtml()))
                            );
                        else
                            html = new object[] { new H2("Parse error"), new P("Your regular expression couldn’t be parsed. If you think it’s a valid regular expression, please send it to us so we can fix our parser.") };
                    }
                    catch (ParseException pe)
                    {
                        html = new DIV { class_ = "regex-error" }._(
                            new DIV { id = "regex-show" }._(
                                new SPAN { class_ = "regex-good" }._(regex.Substring(0, pe.StartIndex)),
                                new SPAN { class_ = "regex-indicator" },
                                new SPAN { class_ = "regex-bad" }._(regex.Substring(pe.StartIndex, pe.EndIndex - pe.StartIndex)),
                                pe.EndIndex != pe.StartIndex ? new SPAN { class_ = "regex-indicator" } : null,
                                new SPAN { class_ = "regex-rest" }._(regex.Substring(pe.EndIndex))
                            ),
                            pe.HtmlMessage
                        );
                    }
                }

                Regex regexObj;
                RegexOptions opt = 0;
                if (single) opt |= RegexOptions.Singleline;
                if (multi) opt |= RegexOptions.Multiline;
                if (ignoreCase) opt |= RegexOptions.IgnoreCase;
                if (ignoreWhitespace) opt |= RegexOptions.IgnorePatternWhitespace;
                try { regexObj = new Regex(regex, opt); }
                catch { invalid = true; }
            }

            return new FORM { method = method.post, action = req.Url.ToFull() }._(
                html,
                new TABLE { class_ = "layout" }._(
                //new COL(), new COL { class_ = "spacer" }, new COL(),
                    new TR(
                        new TD(
                            new H3(Helpers.LabelWithAccessKey("Regular expression", "r", "regex_regex")),
                            new DIV(new TEXTAREA { name = "regex", id = "regex_regex", accesskey = "," }._(regex))
                //),
                //new TD(),
                //new TD(
                //    new H3(Helpers.LabelWithAccessKey("Input text", "n", "regex_input")),
                //    new DIV(new TEXTAREA { name = "input", id = "regex_input" }._(input))
                        )
                    )
                ),
                new DIV(new INPUT { type = itype.checkbox, name = "s", value = "1", id = "regex_s", checked_ = single }, Helpers.LabelWithAccessKey(" Single-line mode", "s", "regex_s"),
                    new DIV { class_ = "explain" }._("If set, ", new CODE("."), " matches any character; if unset, it matches any except newline.")),
                new DIV(new INPUT { type = itype.checkbox, name = "m", value = "1", id = "regex_m", checked_ = multi }, Helpers.LabelWithAccessKey(" Multi-line mode", "m", "regex_m"),
                    new DIV { class_ = "explain" }._("If set, ", new CODE("^"), " and ", new CODE("$"), " match at the beginning and end of any line; if unset, only at the beginning and end of the entire input string.")),
                new DIV(new INPUT { type = itype.checkbox, name = "i", value = "1", id = "regex_i", checked_ = ignoreCase }, Helpers.LabelWithAccessKey(" Ignore case", "i", "regex_i"),
                    new DIV { class_ = "explain" }._("If set, lower-case and upper-case letters are treated as equivalent.")),
                new DIV(new INPUT { type = itype.checkbox, name = "x", value = "1", id = "regex_x", checked_ = ignoreWhitespace }, Helpers.LabelWithAccessKey(" Ignore pattern white-space", "w", "regex_x"),
                    new DIV { class_ = "explain" }._("If set, spaces, tabs and newlines in the regular expression are ignored. Also, the ", new CODE("#"), " character introduces a comment that spans until the end of the line.")),
                new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Go for it", "g")))
            );
        }
    }
}
