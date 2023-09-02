using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RT.Json;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Tuulbox.Regexes;

internal abstract class SourceLocation
{
    public string OriginalSource { get; internal set; }
    public int Index { get; internal set; }
    public SourceLocation(string originalSource, int index) { OriginalSource = originalSource; Index = index; }
    protected SourceLocation() { }

    private int? _line, _column;
    public int Line => _line ??= OriginalSource.Substring(0, Index).Count(ch => ch == '\n') + 1;
    public int Column => _column ??= Index - OriginalSource.Substring(0, Index).LastIndexOf('\n');
}

internal abstract class SourceSpan : SourceLocation
{
    public int Length { get; internal set; }
    public int EndIndex => Index + Length;
    public SourceSpan(string originalSource, int index, int length) : base(originalSource, index) => Length = length;
    protected SourceSpan() : base() { }

    private int? _endLine, _endColumn;
    public int EndLine => _endLine ??= OriginalSource.Substring(0, EndIndex).Count(ch => ch == '\n') + 1;
    public int EndColumn => _endColumn ??= EndIndex - OriginalSource.Substring(0, EndIndex).LastIndexOf('\n');

    public string Source => OriginalSource.Substring(Index, Length);
}

internal abstract class Node : SourceSpan
{
    public static readonly ReadOnlyCollection<string> ControlCharactersExplain = Array.AsReadOnly(Ut.NewArray(
        "the Null character (NUL, U+0000)",
        "the Start of Header character (SOH, U+0001)",
        "the Start of Text character (STX, U+0002)",
        "the End of Text character (ETX, U+0003)",
        "the End of Transmission character (EOT, U+0004)",
        "the Enquiry character (ENQ, U+0005)",
        "the Acknowledgment character (ACK, U+0006)",
        "the Bell character (BEL, U+0007)",
        "the Backspace character (BS, U+0008)",
        "the Horizontal Tab character (HT, U+0009)",
        "the Line Feed character (LF, U+000A)",
        "the Vertical Tab character (VT, U+000B)",
        "the Form Feed character (FF, U+000C)",
        "the Carriage Return character (CR, U+000D)",
        "the Shift Out character (SO, U+000E)",
        "the Shift In character (SI, U+000F)",
        "the Data Link Escape character (DLE, U+0010)",
        "the Device Control 1 character (DC1 or XON, U+0011)",
        "the Device Control 2 character (DC2, U+0012)",
        "the Device Control 3 character (DC3 or XOFF, U+0013)",
        "the Device Control 4 character (DC4, U+0014)",
        "the Negative Acknowledgement character (NAK, U+0015)",
        "the Synchronous idle character (SYN, U+0016)",
        "the End of Transmission Block character (ETB, U+0017)",
        "the Cancel character (CAN, U+0018)",
        "the End of Medium character (EM, U+0019)",
        "the Substitute character (SUB, U+001A)",
        "the Escape character (ESC, U+001B)",
        "the File Separator character (FS, U+001C)",
        "the Group Separator character (GS, U+001D)",
        "the Record Separator character (RS, U+001E)",
        "the Unit Separator character (US, U+001F)"
    ));

    public Node(string source, int index, int length) : base(source, index, length) { }
    protected Node() : base() { }
    public abstract object Html { get; }

    public static string ExplainString(string str, bool concise = false)
    {
        if (str.Length == 1 && str[0] < 32)
            return ControlCharactersExplain[str[0]];
        else if (str.Length == 1)
            return (concise ? "“{0}” (U+{1:X4})" : "the “{0}” character (U+{1:X4})").Fmt(str, char.ConvertToUtf32(str, 0));
        else
            return "the text “{0}”".Fmt(str);
    }
}

internal abstract class TextNode : Node
{
    public string Literal { get; private set; }
    public TextNode(string literal, string source, int index, int length)
        : base(source, index, length) => Literal = literal;
    protected TextNode() : base() { }
}

internal sealed class LiteralNode : TextNode
{
    public bool IsQE { get; private set; }
    public LiteralNode(string literal, string source, int index, int length, bool isQE = false) : base(literal, source, index, length) => IsQE = isQE;
    private LiteralNode() : base() { }
    public override object Html => new SPAN { class_ = "node literal" }
        .Data("text", ExplainString(Literal))
        .Data("isqe", IsQE ? "1" : "0")
        ._(Source == "\n" ? "⏎\n" : Source);
}

internal sealed class EscapedLiteralNode : TextNode
{
    public EscapedLiteralNode(string literal, string source, int index, int length) : base(literal, source, index, length) { }
    private EscapedLiteralNode() : base() { }
    public override object Html => new SPAN { class_ = "node escapedliteral" }
        .Data("char", ExplainString(Literal))
        ._(Source);
}

internal abstract class CharacterClass : IEquatable<CharacterClass>
{
    public static CharacterClass FromEscape(EscapeCode escape) => new CharacterClassEscape(escape);
    public static CharacterClass FromCharacter(char ch) => new CharacterClassCharacter(ch);
    public static CharacterClass FromTo(char from, char to) => new CharacterClassRange(from, to);
    public abstract bool Equals(CharacterClass other);
    public abstract override int GetHashCode();
}

internal sealed class CharacterClassEscape : CharacterClass
{
    public EscapeCode Escape;
    public CharacterClassEscape(EscapeCode escape) => Escape = escape;
    public override string ToString() => Escape switch
    {
        EscapeCode.Digit => "a digit character",
        EscapeCode.NonDigit => "a non-digit character",
        EscapeCode.SpaceCharacter => "a space character (e.g. the space, the tab, the em-space, etc.)",
        EscapeCode.NonSpaceCharacter => "a non-space character",
        EscapeCode.WordCharacter => "a word character",
        EscapeCode.NonWordCharacter => "a non-word character",
        _ => null
    };
    public override bool Equals(CharacterClass other) => (other is CharacterClassEscape escape) && escape.Escape == Escape;
    public override int GetHashCode() => Escape.GetHashCode();
}

internal sealed class CharacterClassCharacter : CharacterClass
{
    public char Char { get; private set; }
    public CharacterClassCharacter(char ch) => Char = ch;
    public override string ToString() => Node.ExplainString(Char.ToString());
    public override bool Equals(CharacterClass other) => (other is CharacterClassCharacter character) && character.Char == Char;
    public override int GetHashCode() => Char.GetHashCode();
}

internal sealed class CharacterClassRange : CharacterClass
{
    public char From { get; private set; }
    public char To { get; private set; }
    public CharacterClassRange(char from, char to) { From = from; To = to; }
    public override string ToString() => "any character between {0} and {1}".Fmt(
            Node.ExplainString(From.ToString(), concise: true),
            Node.ExplainString(To.ToString(), concise: true)
        );
    public override bool Equals(CharacterClass other) => (other is CharacterClassRange range) && range.From == From && range.To == To;
    public override int GetHashCode() => unchecked((From << 16) + To);
}

internal sealed class CharacterClassNode : Node
{
    public CharacterClass[] Classes { get; private set; }
    public bool Negated { get; private set; }
    public CharacterClassNode(CharacterClass[] classes, bool negated, string source, int index, int length)
        : base(source, index, length) { Classes = classes; Negated = negated; }
    private CharacterClassNode() : base() { }
    public override object Html => new SPAN { class_ = "node characterclass" }
        .Data("info", Classes.Distinct().ToJsonList(cl => cl.ToString()))
        .Data("negated", Negated ? 1 : null)
        ._(Source);
}

internal sealed class AnyNode : Node
{
    public AnyNode(string source, int index, int length) : base(source, index, length) { }
    private AnyNode() : base() { }
    public override object Html => new SPAN { class_ = "node any" }._(Source);
}

internal abstract class OneChildNode : Node
{
    public Node Child { get; private set; }
    public OneChildNode(Node child, string source, int index, int length) 
        : base(source, index, length) => Child = child;
    protected OneChildNode() : base() { }
    protected virtual Tag AddData(Tag tag) => tag;
    public override object Html => AddData(new SPAN { class_ = "node " + CssClass }._(
            Source.Substring(0, Child.Index - Index),
            Child.Html,
            Source.Substring(Child.Index + Child.Length - Index)
        ));
    protected abstract string CssClass { get; }
}

internal enum ParenthesisType
{
    Capturing,
    NamedCapturing,
    Grouping,
    BalancingGroup,
    PositiveLookAhead,
    NegativeLookAhead,
    PositiveLookBehind,
    NegativeLookBehind,
    Atomic,
    Flags,
    Conditional
}

internal class ParenthesisNode : OneChildNode
{
    public ParenthesisType Type { get; private set; }
    public ParenthesisNode(ParenthesisType type, Node child, string source, int index, int length)
        : base(child, source, index, length) => Type = type;
    protected override string CssClass => "parenthesis";
    protected override Tag AddData(Tag tag) => base.AddData(tag).Data("type", Type.ToString());
}

internal sealed class NamedParenthesisNode : ParenthesisNode
{
    public string GroupName { get; private set; }
    public NamedParenthesisNode(string name, Node child, string source, int index, int length, ParenthesisType? type = null)
        : base(type ?? ParenthesisType.NamedCapturing, child, source, index, length) => GroupName = name;
    protected override Tag AddData(Tag tag) => base.AddData(tag).Data("groupname", GroupName);
}

internal sealed class BalancingGroupNode : ParenthesisNode
{
    public string Group1Name { get; private set; }
    public string Group2Name { get; private set; }
    public BalancingGroupNode(string name1, string name2, Node child, string source, int index, int length)
        : base(ParenthesisType.BalancingGroup, child, source, index, length) { Group1Name = name1; Group2Name = name2; }
    protected override Tag AddData(Tag tag) => base.AddData(tag)
        .Data("group1name", Group1Name)
        .Data("group2name", Group2Name);
}

[Flags]
internal enum OptionFlags
{
    None = 0,
    IgnoreCase = 1 << 0,
    Multiline = 1 << 1,
    ExplicitCapture = 1 << 2,
    SingleLine = 1 << 3,
    IgnoreWhitespace = 1 << 4
}

internal sealed class FlagsParenthesisNode : ParenthesisNode
{
    public OptionFlags Enable { get; private set; }
    public OptionFlags Disable { get; private set; }
    public FlagsParenthesisNode(OptionFlags enable, OptionFlags disable, Node child, string source, int index, int length)
        : base(ParenthesisType.Flags, child, source, index, length) { Enable = enable; Disable = disable; }
    protected override Tag AddData(Tag tag) => base.AddData(tag)
        .Data("enable", (int)Enable)
        .Data("disable", (int)Disable);
}

internal enum RepeaterType
{
    Star,
    Plus,
    Optional,
    Min,
    Max,
    MinMax,
    Specific
}

internal enum Greediness
{
    Greedy,
    Nongreedy,
    Atomic
}

internal sealed class RepeatOperatorNode : OneChildNode
{
    public RepeaterType Type { get; private set; }
    public int Min { get; private set; }
    public int? Max { get; private set; }
    public Greediness Greediness { get; private set; }
    public RepeatOperatorNode(RepeaterType type, int? min, int? max, Greediness greediness, Node child, string source, int index, int length)
        : base(child, source, index, length) { Type = type; Min = min ?? 0; Max = max; Greediness = greediness; }
    protected override string CssClass => "repeater";
    protected override Tag AddData(Tag tag) => base.AddData(tag)
        .Data("type", Type.ToString())
        .Data("min", Min.ToString())
        .Data("max", Max.ToString())
        .Data("greediness", Greediness.ToString());
}

internal sealed class StartNode : Node
{
    public StartNode(string source, int index, int length) : base(source, index, length) { }
    private StartNode() : base() { }
    public override object Html => new SPAN { class_ = "node start" }._(Source);
}

internal sealed class EndNode : Node
{
    public EndNode(string source, int index, int length) : base(source, index, length) { }
    private EndNode() : base() { }
    public override object Html => new SPAN { class_ = "node end" }._(Source);
}

internal sealed class OrNode : Node
{
    public Node[] Children { get; private set; }
    public OrNode(Node[] children, string source, int index, int length) 
        : base(source, index, length) => Children = children;
    private OrNode() : base() { }
    public override object Html => new SPAN { class_ = "node or" }._(GenerateHtml());
    private IEnumerable<object> GenerateHtml()
    {
        for (int i = 0; i < Children.Length; i++)
        {
            var index = i == 0 ? Index : Children[i - 1].EndIndex;
            yield return OriginalSource.Substring(index, Children[i].Index - index);
            yield return Children[i].Html;
        }
        yield return Source.Substring(Children[Children.Length - 1].EndIndex - Index);
    }
}

internal sealed class ThenNode : Node
{
    public Node[] Children { get; private set; }
    public ThenNode(Node[] children, string source, int index, int length) 
        : base(source, index, length) => Children = children;
    private ThenNode() : base() { }
    public override object Html => new SPAN { class_ = "node then" }._(Children.Select(ch => ch.Html));
}

internal enum EscapeCode
{
    BeginningOfString,      // \A
    WordBoundary,            // \b
    NonWordBoundary,    // \B
    Digit,                              // \d
    NonDigit,                       // \D
    SpaceCharacter,           // \s
    NonSpaceCharacter,    // \S
    WordCharacter,            // \w
    NonWordCharacter,    // \W
    EndOfStringAlmost,    // \Z
    EndOfStringReally,      // \z
}

internal sealed class EscapeCodeNode : Node
{
    public EscapeCode Code { get; private set; }
    public EscapeCodeNode(EscapeCode code, string source, int index, int length) 
        : base(source, index, length) => Code = code;
    private EscapeCodeNode() : base() { }
    public override object Html => new SPAN { class_ = "node escapecode" }
        .Data("code", Code.ToString())
        ._(Source);
}

internal sealed class NamedBackreference : Node
{
    public string Name { get; private set; }
    public NamedBackreference(string name, string source, int index, int length) 
        : base(source, index, length) => Name = name;
    private NamedBackreference() { }
    public override object Html => new SPAN { class_ = "node namedbackref" }
        .Data("name", Name)
        ._(Source);
}

internal sealed class NumberedBackreference : Node
{
    public long Number { get; private set; }
    public NumberedBackreference(long number, string source, int index, int length) 
        : base(source, index, length) => Number = number;
    private NumberedBackreference() { }
    public override object Html => new SPAN { class_ = "node numberedbackref" }
        .Data("number", Number)
        ._(Source);
}
