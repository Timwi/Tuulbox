using System;
using System.Collections.Generic;
using System.Linq;
using RT.Generexes;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

using S = RT.Generexes.Stringerex;
using SN = RT.Generexes.Stringerex<Tuulbox.Regexes.Node>;

namespace Tuulbox.Regexes
{
    static class RegexesUtil
    {
        private static SN _grammar;
        public static SN Grammar
        {
            get
            {
                if (_grammar == null)
                {
                    _grammar = SN.Recursive(generex =>
                    {
                        var specialCharacters = @"[]^$.*+(){}\|?";
                        var number = new S(ch => ch >= '0' && ch <= '9').RepeatGreedy(1);
                        var matchedNumber = number.Process(m => Convert.ToInt32(m.Match));
                        var hexDigit = new S(ch => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'));

                        var literal = new S(ch => !specialCharacters.Contains(ch)).RepeatGreedy(1).Process(m => (Node) new LiteralNode(m.Match, m.OriginalSource, m.Index, m.Length));
                        var escapeCodeChar = Stringerex.Ors(
                            new S(ch => ch >= '{' || @"!""#$%&'()*+,-./:;<=>?@[\]^_`".Contains(ch)).Process(m => m.Match[0]),
                            new S('a').Process(m => '\a'),
                            new S('c').ThenExpect(new S(ch => ch >= 'A' && ch <= 'Z').Process(m => ((char) (m.Match[1] - 'A' + 1))), m => new ParseException(m.Index - 1, m.Index + 1, new P(new CODE(@"\c"), " must be followed by a capital letter between A and Z."))),
                            new S('e').Process(m => '\x1B'),
                            new S('f').Process(m => '\f'),
                            new S('n').Process(m => '\n'),
                            new S('r').Process(m => '\r'),
                            new S('t').Process(m => '\t'),
                            new S('v').Process(m => '\v'),
                            new S('x').ThenExpect(hexDigit.Times(2).Process(m => (char) Convert.ToInt32(m.Match, 16)), m => new ParseException(m.Index - 1, m.Index + 1, new P(new CODE(@"\x"), " must be followed by two hexadecimal digits."))),
                            new S('u').ThenExpect(hexDigit.Times(4).Process(m => (char) Convert.ToInt32(m.Match, 16)), m => new ParseException(m.Index - 1, m.Index + 1, new P(new CODE(@"\u"), " must be followed by four hexadecimal digits."))),
                            new S(ch => ch >= '0' && ch <= '7').RepeatGreedy(1, 3).Process(m => (char) Convert.ToInt32(m.Match, 8)));

                        var escapeCodeGroup = Stringerex.Ors(
                            new S('d').Process(m => EscapeCode.Digit),
                            new S('D').Process(m => EscapeCode.NonDigit),
                            new S('s').Process(m => EscapeCode.SpaceCharacter),
                            new S('S').Process(m => EscapeCode.NonSpaceCharacter),
                            new S('w').Process(m => EscapeCode.WordCharacter),
                            new S('W').Process(m => EscapeCode.NonWordCharacter));

                        var backslash = new S('\\');
                        var escapeOutsideCharacterClass = backslash.ThenExpect(
                            Stringerex.Ors(
                                escapeCodeChar.Process(m => new Func<int, int, Node>((index, length) => new EscapedLiteralNode(m.Result.ToString(), m.OriginalSource, index, length))),
                                Stringerex.Ors(
                                    new S('A').Process(m => EscapeCode.BeginningOfString),
                                    new S('b').Process(m => EscapeCode.WordBoundary),
                                    new S('B').Process(m => EscapeCode.NonWordBoundary),
                                    new S('z').Process(m => EscapeCode.EndOfStringAlmost),
                                    new S('Z').Process(m => EscapeCode.EndOfStringReally),
                                    escapeCodeGroup
                                ).Process(m => new Func<int, int, Node>((index, length) => new EscapeCodeNode(m.Result, m.OriginalSource, index, length))),
                                new S('Q')
                                    .ThenExpect(S.Anything.Process(m => m.Match).Then(@"\E").Atomic(), m => new ParseException(m.Index - 1, m.Index + 1, new P("The ", new CODE(@"\Q"), " escape code introduces a literal that must be terminated with ", new CODE(@"\E"), ".")))
                                    .Process(m => new Func<int, int, Node>((index, length) => new LiteralNode(m.Result, m.OriginalSource, index, length, isQE: true))),
                                new S('k')
                                    .ThenExpect(new S('<').Then(Stringerexes.IdentifierNoPunctuation.Process(m => m.Match)).Then('>'), m => new ParseException(m.Index - 1, m.Index + 1, new P("The correct syntax for this escape code is ", new CODE(@"\k<name>"), ", where the name must consist of letters and digits and start with a letter.")))
                                    .Process(m => new Func<int, int, Node>((index, length) => new NamedBackreference(m.Result, m.OriginalSource, index, length)))
                            ),
                            m => new ParseException(m.Index, m.Index + m.Length, new P("Unrecognized escape code. I know about: ", "acefnrtvxudDsSwWAbBzZQk".Order().Select(ch => new CODE("\\", ch)).InsertBetweenWithAnd<object>(", ", " and "), ". There is also ", new CODE(@"\0"), " through ", new CODE(@"\777"), " (octal) and all the punctuation characters, e.g. ", new CODE(@"\{"), "."))
                        ).Process(m => m.Result(m.Index, m.Length));

                        // In a character class, \b means “backspace character”
                        var charEscapeInCharacterClass = escapeCodeChar.Or(new S('b').Process(m => '\b'));
                        var escapeInCharacterClass = backslash.ThenExpect(
                            Stringerex.Ors(
                                charEscapeInCharacterClass.Process(m => CharacterClass.FromCharacter(m.Result)),
                                escapeCodeGroup.Process(m => CharacterClass.FromEscape(m.Result))),
                            m => new ParseException(m.Index, m.Index + m.Length, new P("Unrecognized escape code. Inside of character classes, I know about: ", "acefnrtvxudDsSwWb".Order().Select(ch => new CODE("\\", ch)).InsertBetweenWithAnd<object>(", ", " and "), ". There is also ", new CODE(@"\0"), " through ", new CODE(@"\777"), " (octal) and all the punctuation characters, e.g. ", new CODE(@"\{"), "."))
                        );

                        var characterClass = new S("[^").Process(m => true).Or(new S('[').Process(m => false)).Atomic().ThenRaw(
                            // Accept a close-square-bracket at the beginning of the character class (e.g., []] is a valid class that matches ']').
                            Stringerex.Ors(

                                // Character range starting with a ']'
                                new S("]-").Then(backslash.Then(charEscapeInCharacterClass).Or(S.Not(']', '\\').Process(m => m.Match[0])).Process(m => CharacterClass.FromTo(']', m.Result))),

                                // Just a ']'
                                new S(']').Process(c => CharacterClass.FromCharacter(']')))
                            .OptionalGreedy()
                            .ThenRaw(Stringerex.Ors(

                                // Character ranges
                                backslash.Then(charEscapeInCharacterClass).Or(S.Not('-', '\\').Process(m => m.Match[0]))
                                    .Then('-')
                                    .ThenRaw(backslash.Then(charEscapeInCharacterClass).Or(S.Not(']', '\\').Process(m => m.Match[0])),
                                        (from, to) => CharacterClass.FromTo(from, to)),

                                // Escape codes
                                escapeInCharacterClass,

                                // Anything else is a literal character.
                                S.Not(']').Process(m => CharacterClass.FromCharacter(m.Match[0]))

                            ).RepeatGreedy(), Enumerable.Concat),
                            (negated, charClasses) => new { Negated = negated, CharacterClass = charClasses.ToArray() })
                            .ThenExpect(']', m => new ParseException(m.Index, m.Index + m.Length, new P("You need to terminate this character class with a ", new CODE("]"), ".")))
                            .Atomic()
                            .Process(m => (Node) new CharacterClassNode(m.Result.CharacterClass, m.Result.Negated, m.OriginalSource, m.Index, m.Length));

                        var start = new S('^').Process(m => (Node) new StartNode(m.OriginalSource, m.Index, m.Length));
                        var end = new S('$').Process(m => (Node) new EndNode(m.OriginalSource, m.Index, m.Length));
                        var any = new S('.').Process(m => (Node) new AnyNode(m.OriginalSource, m.Index, m.Length));

                        var optionFlags = Stringerex.Ors(
                            new S('i').Process(m => OptionFlags.IgnoreCase),
                            new S('m').Process(m => OptionFlags.Multiline),
                            new S('n').Process(m => OptionFlags.ExplicitCapture),
                            new S('s').Process(m => OptionFlags.SingleLine),
                            new S('x').Process(m => OptionFlags.IgnoreWhitespace)
                        ).RepeatGreedy().ProcessRaw(opts => opts.Aggregate(OptionFlags.None, (acc, of) => acc | of));

                        var parentheses = Stringerex.Ors(
                            // Named capturing groups
                            new S("(?<").Then(Stringerexes.IdentifierNoPunctuation.Process(m => m.Match)).Then('>').Atomic()
                                .ThenRaw(generex, (groupName, inner) => new { GroupName = groupName, Inner = inner })
                                .Process(m => new Func<int, int, Node>((index, length) => new NamedParenthesisNode(m.Result.GroupName, m.Result.Inner, m.OriginalSource, index, length))),

                            // Option flags
                            new S("(?").Then(optionFlags).ThenRaw(new S('-').Then(optionFlags).OptionalGreedy(), (enable, disable) => new { Enable = enable, Disable = disable.FirstOrDefault() })
                                .Do(m => m.Match.Contains('-') ? m.Result.Disable != 0 : m.Result.Enable != 0)
                                .Then(':')
                                .ThenRaw(generex, (inf, inner) => new { inf.Enable, inf.Disable, Inner = inner })
                                .Process(m => new Func<int, int, Node>((index, length) => new FlagsParenthesisNode(m.Result.Enable, m.Result.Disable, m.Result.Inner, m.OriginalSource, index, length))),

                            // All types of parentheses that only have a type (and no other parameters)
                            Stringerex.Ors(
                                new S("(?:").Process(m => ParenthesisType.Grouping),
                                new S("(?=").Process(m => ParenthesisType.PositiveLookAhead),
                                new S("(?!").Process(m => ParenthesisType.NegativeLookAhead),
                                new S("(?<=").Process(m => ParenthesisType.PositiveLookBehind),
                                new S("(?<!").Process(m => ParenthesisType.NegativeLookBehind),
                                new S("(?>").Process(m => ParenthesisType.Atomic),
                                new S("(?").Throw<ParenthesisType>(m => new ParseException(m.Index, m.Index + m.Length, Ut.NewArray<object>(
                                    new P("This construct is not valid. Valid constructs beginning with ", new CODE("(?"), " are:"),
                                    new UL(
                                        new LI("Named capturing groups: ", new CODE("(?<name>...)"), " (name must consist of letters and digits and start with a letter)"),
                                        new LI("Option flags: ", new CODE("(?imnsx-imnsx:...)"), "; for example, ", new CODE("(?s-i:...)"), " enables single-line mode and disables case-insensitivity for the inner expression."),
                                        new LI("Non-capturing group: ", new CODE("(?:...)")),
                                        new LI("Positive zero-width look-ahead: ", new CODE("(?=...)")),
                                        new LI("Positive zero-width look-behind: ", new CODE("(?<=...)")),
                                        new LI("Negative zero-width look-ahead: ", new CODE("(?!...)")),
                                        new LI("Negative zero-width look-behind: ", new CODE("(?<!...)")),
                                        new LI("Atomic subexpression: ", new CODE("(?>...)")))))),
                                new S('(').Process(m => ParenthesisType.Capturing)
                            )
                                .Atomic()
                                .ThenRaw(generex, (type, inner) => new { Type = type, Inner = inner })
                                .Process(m => new Func<int, int, Node>((index, length) => new ParenthesisNode(m.Result.Type, m.Result.Inner, m.OriginalSource, index, length)))
                        )
                            .Then(S.Ors(']', '}').Throw(m => new ParseException(m.Index, m.Index + m.Length, Ut.NewArray(
                                new P("You have a closing bracket here that has no matching opening bracket."),
                                new P("If you intended for the character to match literally, precede it with a backslash, i.e.: ", new CODE("\\", m.Match)),
                                new P("Some regular expression dialects allow this use of ", new CODE(m.Match), " and interpret it to match a literal ", new CODE(m.Match), " character. For compatibility, such use is discouraged. Use ", new CODE("\\", m.Match), " instead if this is the intention.")
                            ))).Or(S.Empty))
                            .ThenExpect(')', m => new ParseException(m.Index, m.Index + 1, new P("You need to terminate this parenthesis with a ", new CODE(")"), ".")))
                            .Process(m => m.Result(m.Index, m.Length));

                        var repeater = Stringerex.Ors(
                            new S("*?").Process(m => new { Min = 0, Max = (int?) null, Greedy = Greediness.Nongreedy, Type = RepeaterType.Star }),
                            new S("*+").Process(m => new { Min = 0, Max = (int?) null, Greedy = Greediness.Atomic, Type = RepeaterType.Star }),
                            new S('*').Process(m => new { Min = 0, Max = (int?) null, Greedy = Greediness.Greedy, Type = RepeaterType.Star }),
                            new S("+?").Process(m => new { Min = 1, Max = (int?) null, Greedy = Greediness.Nongreedy, Type = RepeaterType.Plus }),
                            new S("++").Process(m => new { Min = 1, Max = (int?) null, Greedy = Greediness.Atomic, Type = RepeaterType.Plus }),
                            new S('+').Process(m => new { Min = 1, Max = (int?) null, Greedy = Greediness.Greedy, Type = RepeaterType.Plus }),
                            new S("??").Process(m => new { Min = 0, Max = (int?) 1, Greedy = Greediness.Nongreedy, Type = RepeaterType.Optional }),
                            new S('?').Process(m => new { Min = 0, Max = (int?) 1, Greedy = Greediness.Greedy, Type = RepeaterType.Optional }),
                            new S('{').ThenExpect(
                                matchedNumber.Then(',').ThenRaw(matchedNumber.OptionalGreedy().ProcessRaw(m => m.Cast<int?>().FirstOrDefault()), (min, max) => new { Min = min, Max = max, Type = max == null ? RepeaterType.Min : RepeaterType.MinMax })
                                    .Or(new S(',').Then(matchedNumber).ProcessRaw(max => new { Min = 0, Max = (int?) max, Type = RepeaterType.Max }))
                                    .Or(matchedNumber.ProcessRaw(m => new { Min = m, Max = (int?) m, Type = RepeaterType.Specific }))
                                    .Atomic(),
                                m => new ParseException(m.Index, m.Index + m.Length, Ut.NewArray<object>(
                                    new P("The correct syntax for this repeater is one of the following:"),
                                    new UL(
                                        new LI(new CODE("{4,7}"), " — match between 4 and 7 times"),
                                        new LI(new CODE("{,7}"), " — match between 0 and 7 times"),
                                        new LI(new CODE("{4,}"), " — match at least 4 times"),
                                        new LI(new CODE("{4}"), " — match exactly 4 times")
                                    ),
                                    new P("Additionally, the operator may be followed by ", new CODE("?"), " (non-greedy) or ", new CODE("+"), " (atomic)."),
                                    new P("Some regular expression dialects allow this use of ", new CODE("{"), " and interpret it to match a literal ", new CODE("{"), " character. For compatibility, such use is discouraged. Use ", new CODE("\\{"), " instead if this is the intention.")
                                ))
                            ).ThenExpectRaw(
                                Stringerex.Ors(
                                    new S("}?").Process(m => Greediness.Nongreedy),
                                    new S("}+").Process(m => Greediness.Atomic),
                                    new S('}').Process(m => Greediness.Greedy)
                                ).Atomic(),
                                (minmax, greedy) => new { Min = minmax.Min, Max = minmax.Max, Greedy = greedy, Type = minmax.Type },
                                m => new ParseException(m.Index, m.Index + m.Length, new P("You need to terminate this repeater with a ", new CODE("}"), ", ", new CODE("}?"), " or ", new CODE("}+"), "."))
                            )
                        ).Atomic();
                        return Stringerex.Ors(literal, escapeOutsideCharacterClass, characterClass, start, end, any, parentheses,
                            S.Ors('?', '*', '+', '{').Throw<Node>(m => new ParseException(m.Index, m.Index + m.Length, Ut.NewArray(
                                new P("You cannot place a repeater here. A repeater must be preceded by the element it applies to."),
                                new P("If you intended for the character to match literally, precede it with a backslash, i.e.: ", new CODE("\\", m.Match)),
                                m.Match == "{" ? new P("Some regular expression dialects allow this use of ", new CODE("{"), " and interpret it to match a literal ", new CODE("{"), " character. For compatibility, such use is discouraged. Use ", new CODE("\\{"), " instead if this is the intention.") : null
                            )))
                        ).Atomic()
                            .Then(repeater.OptionalGreedy(), (child, repeat) => repeat.Result.Select(r => new RepeatOperatorNode(r.Type, r.Min, r.Max, r.Greedy, child, repeat.OriginalSource, child.Index, child.Length + repeat.Length)).DefaultIfEmpty(child).First())
                            .RepeatGreedy()
                            .Process(match => match.Result.ToArray().Apply(arr => arr.Length == 1 ? arr[0] : new ThenNode(arr, match.OriginalSource, match.Index, match.Length)))
                            .RepeatWithSeparatorGreedy('|')
                            .Process(match => match.Result.ToArray().Apply(arr => arr.Length == 1 ? arr[0] : new OrNode(arr, match.OriginalSource, match.Index, match.Length)));
                    })
                        .Then(S.Ors(
                            S.Ors(')', ']', '}').Throw(m => new ParseException(m.Index, m.Index + m.Length, Ut.NewArray(
                                new P("You have a closing bracket here that has no matching opening bracket."),
                                new P("If you intended for the character to match literally, precede it with a backslash, i.e.: ", new CODE("\\", m.Match)),
                                new P("Some regular expression dialects allow this use of ", new CODE(m.Match), " and interpret it to match a literal ", new CODE(m.Match), " character. For compatibility, such use is discouraged. Use ", new CODE("\\", m.Match), " instead if this is the intention.")
                            ))),
                            S.End
                        ));
                }
                return _grammar;
            }
        }

        private static char unescapeLiteral(char charAfterBackslash)
        {
            switch (charAfterBackslash)
            {
                case '0': return '\0';
                case 'a': return '\a';
                case 'b': return '\b';
                case 't': return '\t';
                case 'n': return '\n';
                case 'v': return '\v';
                case 'f': return '\f';
                case 'r': return '\r';
                case '\\': return '\\';
                case '"': return '"';
                default:
                    //if ((charAfterBackslash >= 'a' && charAfterBackslash <= 'z') || (charAfterBackslash >= 'A' && charAfterBackslash <= 'Z') || (charAfterBackslash >= '0' && charAfterBackslash <= '9'))
                    // throw...
                    return charAfterBackslash;
            }
        }
    }
}
