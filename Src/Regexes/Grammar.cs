using System;
using RT.Util.ExtensionMethods;
using System.Linq;
using RT.Generexes;
using RT.Util;

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
                        var specialCharacters = @"[^$.*+(){\|?";
                        var number = ((S) (ch => ch >= '0' && ch <= '9')).RepeatGreedy(1);
                        var matchedNumber = number.Process(m => Convert.ToInt32(m.Match));
                        var hexDigit = (S) (ch => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'));

                        var literal = new S(ch => !specialCharacters.Contains(ch)).Process(m => (Node) new LiteralNode(false, m.Match, m.OriginalSource, m.Index, m.Length));
                        var escapedLiteral = Stringerex.Ors(
                            new S('\\').Then(Stringerex.Ors(
                                new S(ch => ch >= '{' || @"!""#$%&'()*+,-./:;<=>?@[\]^_`".Contains(ch)).Process(m => m.Match[0].ToString()),
                                new S('a').Process(m => "\a"),
                                new S('c').Then(ch => ch >= 'A' && ch <= 'Z').Process(m => ((char) (m.Match[1] - 'A' + 1)).ToString()),
                                new S('e').Process(m => "\x1B"),
                                new S('f').Process(m => "\f"),
                                new S('n').Process(m => "\n"),
                                new S('r').Process(m => "\r"),
                                new S('t').Process(m => "\t"),
                                new S('v').Process(m => "\v"),
                                new S('x').Then(hexDigit.Times(2).Process(m => char.ConvertFromUtf32(Convert.ToInt32(m.Match, 16)))),
                                new S('u').Then(hexDigit.Times(4).Process(m => char.ConvertFromUtf32(Convert.ToInt32(m.Match, 16)))),
                                new S(ch => ch >= '0' && ch <= '7').RepeatGreedy(1, 3).Process(m => char.ConvertFromUtf32(Convert.ToInt32(m.Match, 8)))
                            )).Process(m => (Node) new EscapedLiteralNode(m.Result, m.OriginalSource, m.Index, m.Length)),
                            new S('\\').Then(Stringerex.Ors(
                                new S('A').Process(m => EscapeCode.BeginningOfString),
                                new S('b').Process(m => EscapeCode.WordBoundary),
                                new S('B').Process(m => EscapeCode.NonWordBoundary),
                                new S('d').Process(m => EscapeCode.Digit),
                                new S('D').Process(m => EscapeCode.NonDigit),
                                new S('s').Process(m => EscapeCode.SpaceCharacter),
                                new S('S').Process(m => EscapeCode.NonSpaceCharacter),
                                new S('w').Process(m => EscapeCode.WordCharacter),
                                new S('W').Process(m => EscapeCode.NonWordCharacter),
                                new S('z').Process(m => EscapeCode.EndOfStringAlmost),
                                new S('Z').Process(m => EscapeCode.EndOfStringReally)
                            )).Process(m => (Node) new EscapeCodeNode(m.Result, m.OriginalSource, m.Index, m.Length)),
                            new S(@"\Q").Then(S.Any.Repeat()).Then(@"\E").Atomic().Process(m => (Node) new LiteralNode(true, m.Match, m.OriginalSource, m.Index, m.Length))
                        ).Atomic();

                        var characterClassInner =
                            // ①: Accept a close-square-bracket at the beginning of the character class (e.g., []] is a valid class that matches ']').
                            ((S) ']').Process(c => CharacterClass.FromCharacter(']')).OptionalGreedy()
                            .ThenRaw(Stringerex.Ors(

                                // ②: Some predefined escapes for character classes
                                Stringerex.Ors(
                                    new S(@"\d").Process(m => EscapeCode.Digit),
                                    new S(@"\D").Process(m => EscapeCode.NonDigit),
                                    new S(@"\s").Process(m => EscapeCode.SpaceCharacter),
                                    new S(@"\S").Process(m => EscapeCode.NonSpaceCharacter),
                                    new S(@"\w").Process(m => EscapeCode.WordCharacter),
                                    new S(@"\W").Process(m => EscapeCode.NonWordCharacter)
                                ).Process(m => CharacterClass.FromEscape(m.Result)),

                                // ③: Character classes. Start with anything that isn’t a minus, but take care of escape sequences
                                Stringerex.Ors(
                                    S.Not('-', '\\').Process(m => m.Match[0]),
                                    new S('\\').Then(S.Any.Process(m => unescapeLiteral(m.Match[0])))
                                ).Then('-').Then(S.Not(']'), (from, m) => CharacterClass.FromTo(from, m.Match[0])),

                                // ④: All other character escapes
                                new S('\\').Then(S.Any.Process(m => CharacterClass.FromCharacter(unescapeLiteral(m.Match[0])))),

                                // ⑤: Anything else is a literal character.
                                S.Any.Process(m => CharacterClass.FromCharacter(m.Match[0]))

                            ).Repeat(min: 1), Enumerable.Concat)
                            .Then(']');
                        var characterClass = Stringerex.Ors(
                            ((S) "[^").Then(characterClassInner).Process(m => (Node) new CharacterClassNode(m.Result.ToArray(), true, m.OriginalSource, m.Index, m.Length)),
                            ((S) '[').Then(characterClassInner).Process(m => (Node) new CharacterClassNode(m.Result.ToArray(), false, m.OriginalSource, m.Index, m.Length))
                        );

                        var start = ((S) '^').Process(m => (Node) new StartNode(m.OriginalSource, m.Index, m.Length));
                        var end = ((S) '$').Process(m => (Node) new EndNode(m.OriginalSource, m.Index, m.Length));
                        var any = ((S) '.').Process(m => (Node) new AnyNode(m.OriginalSource, m.Index, m.Length));

                        var parentheses = Stringerex.Ors(
                            // All types of parentheses that only have a type (and no other parameters)
                            Stringerex.Ors(
                                new S("(?:").Process(m => ParenthesisType.Grouping),
                                new S("(?=").Process(m => ParenthesisType.PositiveLookAhead),
                                new S("(?<=").Process(m => ParenthesisType.PositiveLookBehind),
                                new S("(?!").Process(m => ParenthesisType.NegativeLookAhead),
                                new S("(?<!").Process(m => ParenthesisType.NegativeLookBehind),
                                new S("(?>").Process(m => ParenthesisType.Atomic),
                                new S('(').Process(m => ParenthesisType.Capturing)
                            )
                                .Atomic()
                                .ThenRaw(generex, (type, inner) => new { Type = type, Inner = inner })
                                .Then(')')
                                .Process(m => (Node) new ParenthesisNode(m.Result.Type, m.Result.Inner, m.OriginalSource, m.Index, m.Length)),

                            // Named capturing groups
                            new S("(?<").Then(Stringerexes.IdentifierNoPunctuation.Process(m => m.Match)).Then('>').Atomic()
                                .ThenRaw(generex, (groupName, inner) => new { GroupName = groupName, Inner = inner })
                                .Then(')')
                                .Process(m => (Node) new NamedParenthesisNode(m.Result.GroupName, m.Result.Inner, m.OriginalSource, m.Index, m.Length))
                        );

                        var repeater = Stringerex.Ors(
                            new S("*?").Process(m => new { Min = 0, Max = (int?) null, Greedy = Greediness.Nongreedy, Type = RepeaterType.Star }),
                            new S("*+").Process(m => new { Min = 0, Max = (int?) null, Greedy = Greediness.Atomic, Type = RepeaterType.Star }),
                            new S('*').Process(m => new { Min = 0, Max = (int?) null, Greedy = Greediness.Greedy, Type = RepeaterType.Star }),
                            new S("+?").Process(m => new { Min = 1, Max = (int?) null, Greedy = Greediness.Nongreedy, Type = RepeaterType.Plus }),
                            new S("++").Process(m => new { Min = 1, Max = (int?) null, Greedy = Greediness.Atomic, Type = RepeaterType.Plus }),
                            new S('+').Process(m => new { Min = 1, Max = (int?) null, Greedy = Greediness.Greedy, Type = RepeaterType.Plus }),
                            new S("??").Process(m => new { Min = 0, Max = (int?) 1, Greedy = Greediness.Nongreedy, Type = RepeaterType.Optional }),
                            new S('?').Process(m => new { Min = 0, Max = (int?) 1, Greedy = Greediness.Greedy, Type = RepeaterType.Optional }),
                            new S('{').Then(
                                matchedNumber.Then(',').ThenRaw(matchedNumber.OptionalGreedy().ProcessRaw(m => m.Cast<int?>().FirstOrDefault()), (min, max) => new { Min = min, Max = max, Type = max == null ? RepeaterType.Min : RepeaterType.MinMax })
                                    .Or(((S) ',').Then(matchedNumber).ProcessRaw(max => new { Min = 0, Max = (int?) max, Type = RepeaterType.Max }))
                                    .Or(matchedNumber.ProcessRaw(m => new { Min = m, Max = (int?) m, Type = RepeaterType.Specific }))
                            ).ThenRaw(
                                Stringerex.Ors(new S("}?").Process(m => Greediness.Nongreedy), new S("}+").Process(m => Greediness.Atomic), new S('}').Process(m => Greediness.Greedy)).Atomic(),
                                (minmax, greedy) => new { Min = minmax.Min, Max = minmax.Max, Greedy = greedy, Type = minmax.Type }
                            )
                        ).Atomic();
                        return Stringerex.Ors(literal, escapedLiteral, characterClass, start, end, any, parentheses).Atomic()
                            .Then(repeater.OptionalGreedy(), (child, repeat) => repeat.Result.Select(r => new RepeatOperatorNode(r.Type, r.Min, r.Max, r.Greedy, child, repeat.OriginalSource, child.Index, child.Length + repeat.Length)).DefaultIfEmpty(child).First())
                            .RepeatGreedy()
                            .Process(match => { var arr = match.Result.ToArray(); return arr.Length == 1 ? arr[0] : new ThenNode(arr, match.OriginalSource, match.Index, match.Length); })
                            .RepeatWithSeparatorGreedy('|')
                            .Process(match => { var arr = match.Result.ToArray(); return arr.Length == 1 ? arr[0] : new OrNode(arr, match.OriginalSource, match.Index, match.Length); });
                    });
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
