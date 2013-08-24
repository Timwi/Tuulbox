using System.Text.RegularExpressions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using Tuulbox.Regexes;

namespace Tuulbox.Tools
{
    sealed class RegexesTool : ITool
    {
        string ITool.Name { get { return "Regular expressions"; } }
        string ITool.Url { get { return "/regexes"; } }
        string ITool.Keywords { get { return "regular expressions regexes regexps regex match text pattern"; } }
        string ITool.Description { get { return "Helps you debug a regular expression by determining where it stops matching."; } }
        string ITool.Js { get { return _js ?? (_js = generateJs()); } }
        string ITool.Css
        {
            get
            {
                return @"
/*
.node { display: inline-block; padding: .1em .25em; margin: .1em .25em; border: 1px solid #eee; border-radius: 5px; }
.node:hover { border-color: #888; }
*/

#regex-show { color: black; font-size: 250%; }
#regex-show:hover .node { color: #ddd; }
#regex-show:hover .node.innerhover:hover { color: #000; }
#regex-show:hover .node.innerhover:hover .node { color: #68a; }

#regex-explain {
    position: absolute;
    display: none;
    padding: .5em 1.2em;
    background-image: linear-gradient(bottom, #def 0%, #8ad 100%);
    background-image: -o-linear-gradient(bottom, #def 0%, #8ad 100%);
    background-image: -moz-linear-gradient(bottom, #def 0%, #8ad 100%);
    background-image: -webkit-linear-gradient(bottom, #def 0%, #8ad 100%);
    background-image: -ms-linear-gradient(bottom, #def 0%, #8ad 100%);
    border: 2px solid #666;
    border-radius: 1em;
    color: black;
    box-shadow: 3px 2px 5px rgba(0, 0, 0, .7);
    text-shadow: 0 0 5px white;
    max-width: 25em;
}

#regex-explain h3, #regex-explain p { margin: .7em 0; }
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

    var explains = [
        [ 'then', '<h3>Literal</h3><p>Matches the specified text.</p>' ],
        [ 'literal', function(elem) { return '<h3>Literal</h3><p>' + (elem.data('istext') ? 'Matches the text between \\Q and \\E.' : 'Matches the specified text.'); } ],
        [ 'escapedliteral', function(elem) { return '<h3>Escaped literal</h3><p>Matches ' + elem.data('char') + '.</p>'; } ],
        [ 'or', '<h3>Or</h3><p>Matches any one of the several subexpressions.</p><p class=""extra"">Also known as “alternation”.</p>' ],

        [ 'parenthesis', function(elem)
        {
            switch (elem.data('type'))
            {
                case 'Capturing': return '<h3>Capture</h3><p>Captures (remembers) the subexpression. The string matched by the subexpression is typically stored in a variable.<p>To group expressions without capturing their matches, use <code>(?:...)</code>.';
                case 'Grouping': return '<h3>Group</h3><p>Groups several literals and operators into a subexpression, typically to apply an operator such as <code>*</code> on the entire subexpression.<p>To capture the match of a subexpression into a variable, omit the <code>?:</code>.';
                case 'PositiveLookAhead': return '<h3>Positive zero-width look-ahead assertion</h3><p>Checks if the text following the current position matches the subexpression, but doesn’t consume any of it; if the subexpression matches, returns a zero-width match.<p>To check if the following text does <em>not</em> match the subexpression, use <code>(?!...)</code>.<p>To check if the <em>earlier</em> text matches the subexpression (look-<em>behind</em>), use <code>(?<=...)</code>.';
                case 'NegativeLookAhead': return '<h3>Negative zero-width look-ahead assertion</h3><p>Checks if the text following the current position <em>does not</em> match the subexpression. If the subexpression does not match, returns a zero-width match.<p>To check if the following text <em>does</em> match the subexpression, use <code>(?=...)</code>.<p>To check if the <em>earlier</em> text does not match the subexpression (look-<em>behind</em>), use <code>(?<!...)</code>.';
                case 'PositiveLookBehind': return '<h3>Positive zero-width look-behind assertion</h3><p>Checks if the text before the current position (i.e. the text matched by the regular expression so far, and possibly more text before that) matches the subexpression. If the subexpression matches, returns a zero-width match.<p>To check if the earlier text does <em>not</em> match the subexpression, use <code>(?<!...)</code>.<p>To check if the <em>following</em> text matches the subexpression (look-<em>ahead</em>), use <code>(?=...)</code>.';
                case 'NegativeLookBehind': return '<h3>Negative zero-width look-behind assertion</h3><p>Checks if the text before the current position (i.e. the text matched by the regular expression so far, and possibly more text before that) <em>does not</em> the subexpression. If the subexpression does not match, returns a zero-width match.<p>To check if the earlier text <em>does</em> match the subexpression, use <code>(?<=...)</code>.<p>To check if the <em>following</em> text does not match the subexpression (look-<em>ahead</em>), use <code>(?!...)</code>.';
                case 'Atomic': return '<h3>Atomic match</h3><p>Allows the subexpression to use only the first match it finds. The subexpression can use backtracking to find that first match, but once it has found it, it must stick to it; if the rest of the regular expression (after the atomic operator) then does not match, the subexpression is not allowed to backtrack.';
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
                    case 'Star': op = '*'; html += '<p>Matches the subexpression any number of times (including zero times).<p>To exclude zero times, use <code>+</code> instead of <code>*</code>.'; break;
                    case 'Plus': op = '+'; html += '<p>Matches the subexpression any number of times (but at least once).<p>To exclude zero times, use <code>*</code> instead of <code>+</code>.'; break;
                    case 'Min': op = '{' + elem.data('min') + ',}'; html += '<p>Matches the subexpression any number of times, but at least ' + elem.data('min') + ' times.'; break;
                    case 'Max': op = '{,' + elem.data('max') + '}'; html += '<p>Matches the subexpression up to ' + elem.data('max') + ' times (including possibly zero times).<p>To exclude zero times, use <code>{1,' + elem.data('max') + '}</code> instead. To include infinite number of times, use <code>*</code> instead.'; break;
                    case 'MinMax': op = '{' + elem.data('min') + ',' + elem.data('max') + '}'; html += '<p>Matches the subexpression between ' + elem.data('min') + ' and ' + elem.data('max') + ' times.<p>To include infinite number of times, use <code>{' + elem.data('min') + ',}</code> instead.'; break;
                    case 'Specified': op = '{' + elem.data('min') + '}'; html += '<p>Matches the subexpression exactly ' + elem.data('min') + ' times.<p>To match the subexpression a variable number of times, use (for instance) <code>{5,10}</code> for 5 to 10 times.'; break;
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

        object ITool.Handle(HttpRequest req)
        {
            string regex = null, input = null;
            bool single = true, multi = false, ignoreCase = false, ignoreWhitespace = false;
            bool invalid = false;
            object html = null;

            if (req.Method == HttpMethod.Post)
            {
                regex = req.Post["regex"].Value;
                input = req.Post["input"].Value;
                single = req.Post["s"].Value == "1";
                multi = req.Post["m"].Value == "1";
                ignoreCase = req.Post["i"].Value == "1";
                ignoreWhitespace = req.Post["x"].Value == "1";

                if (regex != null)
                {
                    var match = RegexesUtil.Grammar.MatchExact(regex.ToCharArray());
                    if (match != null)
                        html = Ut.NewArray<object>(
                            new P("Here is a breakdown of your regular expression. Hover the mouse over any item to get information about it."),
                            new DIV(new SPAN { id = "regex-show" }._(match.Result.ToHtml()))
                        );
                    else
                        html = new object[] { new H2("Parse error"), new P("Your regular expression couldn’t be parsed! If you think it’s a valid regular expression, please send it to us so we can fix our parser!") };
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

            return new FORM { method = method.post, action = req.Url.ToHref() }._(
                html,
                new TABLE { class_ = "layout" }._(new COL(), new COL { class_ = "spacer" }, new COL(), new TR(
                    new TD(
                        new H3(Helpers.LabelWithAccessKey("Regular expression", "r", "regex_regex")),
                        new DIV(new TEXTAREA { name = "regex", id = "regex_regex" }._(regex))
                    ),
                    new TD(),
                    new TD(
                        new H3(Helpers.LabelWithAccessKey("Input text", "n", "regex_input")),
                        new DIV(new TEXTAREA { name = "input", id = "regex_input" }._(input))
                    )
                )),
                new DIV(new INPUT { type = itype.checkbox, name = "s", value = "1", id = "regex_s", checked_ = single }, Helpers.LabelWithAccessKey(" Single-line mode", "s", "regex_s"),
                    new DIV { class_ = "explain" }._("If set, ", new CODE("."), " matches any character; if unset, it matches any except newline.")),
                new DIV(new INPUT { type = itype.checkbox, name = "m", value = "1", id = "regex_m", checked_ = multi }, Helpers.LabelWithAccessKey(" Multi-line mode", "m", "regex_m"),
                    new DIV { class_ = "explain" }._("If set, ", new CODE("^"), " and ", new CODE("$"), " match at the beginning and end of any line; if unset, only at the beginning and end of the entire input string.")),
                new DIV(new INPUT { type = itype.checkbox, name = "i", value = "1", id = "regex_i", checked_ = ignoreCase }, Helpers.LabelWithAccessKey(" Ignore case", "i", "regex_i"),
                    new DIV { class_ = "explain" }._("If set, lower-case and upper-case letters are treated as equivalent.")),
                new DIV(new INPUT { type = itype.checkbox, name = "x", value = "1", id = "regex_x", checked_ = ignoreWhitespace }, Helpers.LabelWithAccessKey(" Ignore pattern white-space", "w", "regex_x"),
                    new DIV { class_ = "explain" }._("If set, spaces, tabs and newlines in the regular expression are ignored. Also, the ", new CODE("#"), " character introduces a comment that spans until the end of the line.")),
                new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(new U("G"), "o for it"))
            );
        }
    }
}
