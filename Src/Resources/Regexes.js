$(function () {
    var $curTooltip = null;

    var htmlEscape = function (str) {
        return str.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;').replace("'", ' &#39; ');
    };

    var explains = [
        ['then', '<h3>Literal</h3><p>Matches the specified text.</p>'],
        ['literal', function (elem) { return '<h3>Literal</h3><p>Matches ' + htmlEscape(elem.data('text')) + '.' + (elem.data('isqe') ? '<p>The <code>\\Q</code>...<code>\\E</code> operator is not supported by .NET.' : ''); }],
        ['escapedliteral', function (elem) { return '<h3>Escaped literal</h3><p>Matches ' + elem.data('char') + '.</p>'; }],
        ['or', '<h3>Or</h3><p>Matches any one of the several subexpressions.</p><p class="extra">Also known as �alternation�.</p>'],
        ['any', '<h3>Any character</h3><p>In single-line mode, matches a single character. Without single-line mode, matches a single character except newlines.</p>'],
        ['start', '<h3>Beginning of line or string</h3><p>In multi-line mode, matches at the beginning of a line. Without multi-line mode, matches only at the beginning of the entire input string.</p>'],
        ['end', '<h3>End of line or string</h3><p>In multi-line mode, matches at the end of a line. Without multi-line mode, matches either at the end of the entire input string, or before a <code>\\n</code> (newline) character at the end of the entire input string.</p>'],
        ['namedbackref', '<h3>Named backreference</h3><p>Matches only the same text that was previously matched by the named capturing parenthesis with the same name.'],
        ['numberedbackref', '<h3>Numbered backreference</h3><p>Matches only the same text that was previously matched by the unnamed capturing parenthesis identified by the number. The first such capturing parenthesis has the number 1. They are ordered by where they start, so in the regular expression <code>((.).)</code>, <code>\\1</code> refers to the outer group, <code>\\2</code> to the inner.'],

        ['characterclass', function (elem) {
            var info = elem.data('info');
            var negated = elem.data('negated');
            var str = negated
                ? '<h3>Negated character class</h3><p>Matches a single character that is not any of the following:</p><ul>'
                : '<h3>Character class</h3><p>Matches any of the following characters:</p><ul>';
            for (var i = 0; i < info.length; i++)
                str += '<li>' + $('<div>').text(info[i]).html() + '</li>';
            return str + '</ul>';
        }],

        ['escapecode', function (elem) {
            switch (elem.data('code')) {
                case 'BeginningOfString': return '<h3>Beginning of string</h3><p>Matches only at the beginning of the string, irrespective of the multi-line setting.</p>';
                case 'WordBoundary': return '<h3>Word boundary</h3><p>Matches at places where the string changes from a word character to a non-word character or vice versa, or at the beginning or end of the string if it straddles a word character.</p><p>For example, in the string:</p><blockquote><code>"he (or she)"</code></blockquote><p>there are six word boundaries, here marked with vertical lines:</p><blockquote><code>"</code>|<code>he</code>|<code> (</code>|<code>or</code>|<code> </code>|<code>she</code>|<code>)"</code></blockquote>';
                case 'NonWordBoundary': return '<h3>Non-word boundary</h3><p>Matches at places where there is not a word boundary. See <code>\\b</code> for an example.</p>';
                case 'Digit': return '<h3>Digit</h3><p>Matches any digit character, including digits from other scripts (such as the Hindi numbers).</p>';
                case 'NonDigit': return '<h3>Non-digit</h3><p>Matches any character that is not a digit character.</p>';
                case 'SpaceCharacter': return '<h3>Space character</h3><p>Matches any space character, including the tab, the em space, etc.</p>';
                case 'NonSpaceCharacter': return '<h3>Non-space character</h3><p>Matches any character that is not a space character (which includes the tab, the em space, etc.).</p>';
                case 'WordCharacter': return '<h3>Word character</h3><p>Matches any word character, which includes letters, digits, and connector punctuation (e.g. the underscore, U+005F).</p>';
                case 'NonWordCharacter': return '<h3>Non-word character</h3><p>Matches any character that is not a word character (which includes letters, digits, and connector punctuation such as the underscore, U+005F).</p>';
                case 'EndOfStringAlmost': return '<h3>End of string</h3><p>Matches either at the end of the entire input string, or before a <code>\\n</code> (newline) character at the end of the entire input string, irrespective of the multi-line setting.</p><p>To match only the end of the string, use <code>\\z</code> (lower-case).</p>';
                case 'EndOfStringReally': return '<h3>End of string only</h3><p>Matches only at the end of the entire input string, irrespective of the multi-line setting.</p><p>To also match before a <code>\\n</code> (newline) character at the end of the input string, use <code>\\Z</code> (upper-case).</p>';
            }
        }],

        ['parenthesis', function (elem) {
            function flagList(flags) {
                var list = [];
                if (flags & 1) list.push('<li><b>Ignore case</b>');
                if (flags & 2) list.push('<li><b>Multi-line mode</b> � changes the meaning of <code>^</code> and <code>$</code>.');
                if (flags & 4) list.push('<li><b>Explicit capture</b> � when on, plain parentheses do not capture their matches automatically. This option is only supported by .NET.');
                if (flags & 8) list.push('<li><b>Single-line mode</b> � changes the meaning of <code>.</code>.');
                if (flags & 16) list.push('<li><b>Ignore whitespace</b> � when on, ignores whitespace and comments initiated by <code>#</code> until the end of the line.');
                return '<ul>' + list.join('') + '</ul>';
            }
            switch (elem.data('type')) {
                case 'Capturing': return '<h3>Capture</h3><p>Captures (remembers) the subexpression. The string matched by the subexpression is typically stored in a variable.<p>To group expressions without capturing their matches, use <code>(?:...)</code>.';
                case 'NamedCapturing': return '<h3>Named capture</h3><p>Captures (remembers) the subexpression and gives the string matched by the subexpression the name �' + elem.data('groupname') + '�.<p>To group expressions without capturing their matches, use <code>(?:...)</code>.';
                case 'Grouping': return '<h3>Group</h3><p>Groups several literals and operators into a subexpression, typically to apply an operator such as <code>*</code> on the entire subexpression.<p>To capture the match of a subexpression into a variable, omit the <code>?:</code>.';
                case 'PositiveLookAhead': return '<h3>Positive zero-width look-ahead assertion</h3><p>Checks if the text following the current position matches the subexpression, but doesn�t consume any of it; if the subexpression matches, returns a zero-width match.<p>To check if the following text does <em>not</em> match the subexpression, use <code>(?!...)</code>.<p>To check if the <em>earlier</em> text matches the subexpression (look-<em>behind</em>), use <code>(?&lt;=...)</code>.';
                case 'NegativeLookAhead': return '<h3>Negative zero-width look-ahead assertion</h3><p>Checks if the text following the current position <em>does not</em> match the subexpression. If the subexpression does not match, returns a zero-width match.<p>To check if the following text <em>does</em> match the subexpression, use <code>(?=...)</code>.<p>To check if the <em>earlier</em> text does not match the subexpression (look-<em>behind</em>), use <code>(?&lt;!...)</code>.';
                case 'PositiveLookBehind': return '<h3>Positive zero-width look-behind assertion</h3><p>Checks if the text before the current position (i.e. the text matched by the regular expression so far, and possibly more text before that) matches the subexpression. If the subexpression matches, returns a zero-width match.<p>To check if the earlier text does <em>not</em> match the subexpression, use <code>(?&lt;!...)</code>.<p>To check if the <em>following</em> text matches the subexpression (look-<em>ahead</em>), use <code>(?=...)</code>.';
                case 'NegativeLookBehind': return '<h3>Negative zero-width look-behind assertion</h3><p>Checks if the text before the current position (i.e. the text matched by the regular expression so far, and possibly more text before that) <em>does not</em> match the subexpression. If the subexpression does not match, returns a zero-width match.<p>To check if the earlier text <em>does</em> match the subexpression, use <code>(?&lt;=...)</code>.<p>To check if the <em>following</em> text does not match the subexpression (look-<em>ahead</em>), use <code>(?!...)</code>.';
                case 'Atomic': return '<h3>Atomic match</h3><p>Allows the subexpression to use only the first match it finds. The subexpression can use backtracking to find that first match, but once it has found it, it must stick to it; if the rest of the regular expression (after the atomic operator) then does not match, the subexpression is not allowed to backtrack.';
                case 'Conditional': return '<h3>Conditional match</h3><p>Matches the subexpression only if a group by the name �' + elem.data('groupname') + '� matched.';

                case 'BalancingGroup':
                    var group1 = elem.data('group1name');
                    return '<h3>Balancing group capture</h3><p>Enables the matching of balanced parentheses/brackets opened by the capturing group named �' + elem.data('group2name') + '�' +
                        (group1 ? ' and gives the string matched by the subexpression the name �' + group1 + '�' : '') + '.';

                case 'Flags':
                    var enable = elem.data('enable');
                    var disable = elem.data('disable');
                    if (enable) {
                        return '<h3>Enable/disable options</h3><p><b>Enables</b> the following options:' + flagList(enable) +
                            (disable ? '<p>and <b>disables</b> the following options:' + flagList(disable) : '');
                    }
                    else {
                        return '<h3>Enable/disable options</h3><p><b>Disables</b> the following options:' + flagList(disable);
                    }
            }
        }],

        ['repeater', function (elem) {
            var html;
            if (elem.data('type') == 'Optional') {
                html = '<h3>Optional</h3>';
                html += '<p>Makes the subexpression optional; in other words, it may match once or not at all.';
                html += '<p>This is equivalent to <code>{0,1}' + (elem.data('greediness') == 'Greedy' ? '' : '?') + '</code>.';
                if (elem.data('greediness') == 'Nongreedy')
                    html += '<p>The operator is <b>non-greedy</b>: it matches the empty string first, and if no match is found for the rest of the regular expression, matches the subexpression and then tries again. To use the greedy variant, use <code>?</code> instead of <code>??</code>.';
                else
                    html += '<p>The operator is <b>greedy</b>: it matches the subexpression first, and if no match is found for the rest of the regular expression, matches the empty string and then tries again. To use the non-greedy variant, use <code>??</code>.';
            }
            else {
                html = '<h3>Repeater</h3>';
                var op = '';
                switch (elem.data('type')) {
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

    var processNodeHover = function () {
        var elem = $(':hover').last();
        var ex = $('#regex-explain');
        if (!ex.length)
            ex = $('<div id="regex-explain"></div>').appendTo(document.body);
        if (elem.hasClass('node')) {
            $('.innerhover').removeClass('innerhover');
            elem.addClass('innerhover');
            var html;
            for (var i = 0; i < explains.length && !html; i++)
                if (elem.hasClass('node') && elem.hasClass(explains[i][0]))
                    html = typeof explains[i][1] === 'function' ? explains[i][1](elem) : explains[i][1];
            ex.html(html || '<p>We don�t have any explanation for this element yet.');
            ex.show().position({ my: 'left top', at: 'left bottom', of: elem });
        }
        else
            ex.hide();
    };

    $('.node').hover(processNodeHover, function () { window.setTimeout(processNodeHover, 1); });
});