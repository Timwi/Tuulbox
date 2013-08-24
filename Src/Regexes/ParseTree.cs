using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.TagSoup;

namespace Tuulbox.Regexes
{
    class SourceLocation
    {
        public string OriginalSource { get; internal set; }
        public int Index { get; internal set; }
        public SourceLocation(string origSource, int index) { OriginalSource = origSource; Index = index; }
        protected SourceLocation() { }

        private int? _line, _column;
        public int Line { get { return (_line ?? (_line = OriginalSource.Substring(0, Index).Count(ch => ch == '\n') + 1)).Value; } }
        public int Column { get { return (_column ?? (_column = Index - OriginalSource.Substring(0, Index).LastIndexOf('\n'))).Value; } }
    }

    class SourceSpan : SourceLocation
    {
        public int Length { get; internal set; }
        public int EndIndex { get { return Index + Length; } }
        public SourceSpan(string origSource, int index, int length) : base(origSource, index) { Length = length; }
        protected SourceSpan() : base() { }

        private int? _endLine, _endColumn;
        public int EndLine { get { return (_endLine ?? (_endLine = OriginalSource.Substring(0, EndIndex).Count(ch => ch == '\n') + 1)).Value; } }
        public int EndColumn { get { return (_endColumn ?? (_endColumn = EndIndex - OriginalSource.Substring(0, EndIndex).LastIndexOf('\n'))).Value; } }

        public string Source { get { return OriginalSource.Substring(Index, Length); } }
    }

    abstract class Node : SourceSpan
    {
        public Node(string source, int index, int length) : base(source, index, length) { }
        protected Node() : base() { }
        public abstract object ToHtml();
    }

    sealed class LiteralNode : Node
    {
        public bool IsText { get; private set; }
        public string Literal { get; private set; }
        public LiteralNode(bool isText, string literal, string source, int index, int length) : base(source, index, length) { IsText = isText; Literal = literal; }
        private LiteralNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node literal" }.Data("text", IsText ? "1" : "0")._(Source); }
    }

    sealed class EscapedLiteralNode : Node
    {
        public string Literal { get; private set; }
        public EscapedLiteralNode(string literal, string source, int index, int length) : base(source, index, length) { Literal = literal; }
        private EscapedLiteralNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node escapedliteral" }.Data("char", explainChar())._(Source); }
        private string explainChar()
        {
            if (Literal.Length == 1 && Literal[0] < 32)
                return _controlCharactersExplain[Literal[0]];
            return "the “{0}” character (U+{1:X4})".Fmt(Literal, char.ConvertToUtf32(Literal, 0));
        }
        private string[] _controlCharactersExplain = Ut.NewArray<string>(
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
            "the Line feed character (LF, U+000A)",
            "the Vertical Tab character (VT, U+000B)",
            "the Form feed character (FF, U+000C)",
            "the Carriage return character (CR, U+000D)",
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
        );
    }

    sealed class CharacterClass
    {
        public char From { get; private set; }
        public char To { get; private set; }
        public CharacterClass(char from, char to) { From = from; To = to; }
    }

    sealed class CharacterClassNode : Node
    {
        public CharacterClass[] Classes { get; private set; }
        public CharacterClassNode(CharacterClass[] classes, string source, int index, int length) : base(source, index, length) { Classes = classes; }
        private CharacterClassNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node characterclass" }._(Source); }
    }

    sealed class AnyNode : Node
    {
        public AnyNode(string source, int index, int length) : base(source, index, length) { }
        private AnyNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node any" }._(Source); }
    }

    abstract class OneChildNode : Node
    {
        public Node Child { get; private set; }
        public OneChildNode(Node child, string source, int index, int length) : base(source, index, length) { Child = child; }
        protected OneChildNode() : base() { }
        protected virtual Tag addData(Tag tag) { return tag; }
        public override object ToHtml()
        {
            return addData(new SPAN { class_ = "node " + CssClass }._(
                Source.Substring(0, Child.Index - Index),
                Child.ToHtml(),
                Source.Substring(Child.Index + Child.Length - Index)
            ));
        }
        protected abstract string CssClass { get; }
    }

    enum ParenthesisType
    {
        Capturing,
        Grouping,
        PositiveLookAhead,
        NegativeLookAhead,
        PositiveLookBehind,
        NegativeLookBehind,
        Atomic
    }

    sealed class ParenthesisNode : OneChildNode
    {
        public ParenthesisType Type { get; private set; }
        public ParenthesisNode(ParenthesisType type, Node child, string source, int index, int length)
            : base(child, source, index, length) { Type = type; }
        protected override string CssClass { get { return "parenthesis"; } }
        protected override Tag addData(Tag tag) { return base.addData(tag).Data("type", Type.ToString()); }
    }

    enum RepeaterType
    {
        Star,
        Plus,
        Optional,
        Min,
        Max,
        MinMax,
        Specific
    }

    enum Greediness
    {
        Greedy,
        Nongreedy,
        Atomic
    }

    sealed class RepeatOperatorNode : OneChildNode
    {
        public RepeaterType Type { get; private set; }
        public int Min { get; private set; }
        public int? Max { get; private set; }
        public Greediness Greediness { get; private set; }
        public RepeatOperatorNode(RepeaterType type, int min, int? max, Greediness greediness, Node child, string source, int index, int length)
            : base(child, source, index, length) { Type = type; Min = min; Max = max; Greediness = greediness; }
        protected override string CssClass { get { return "repeater"; } }
        protected override Tag addData(Tag tag) { return base.addData(tag).Data("type", Type.ToString()).Data("min", Min.ToString()).Data("max", Max.ToString()).Data("greediness", Greediness.ToString()); }
    }

    sealed class StartNode : Node
    {
        public StartNode(string source, int index, int length) : base(source, index, length) { }
        private StartNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node start" }._(Source); }
    }

    sealed class EndNode : Node
    {
        public EndNode(string source, int index, int length) : base(source, index, length) { }
        private EndNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node end" }._(Source); }
    }

    sealed class OrNode : Node
    {
        public Node[] Children { get; private set; }
        public OrNode(Node[] children, string source, int index, int length) : base(source, index, length) { Children = children; }
        private OrNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node or" }._(toHtml()); }
        private IEnumerable<object> toHtml()
        {
            for (int i = 0; i < Children.Length; i++)
            {
                var index = i == 0 ? Index : Children[i - 1].EndIndex;
                yield return OriginalSource.Substring(index, Children[i].Index - index);
                yield return Children[i].ToHtml();
            }
            yield return Source.Substring(Children[Children.Length - 1].EndIndex - Index);
        }
    }

    sealed class ThenNode : Node
    {
        public Node[] Children { get; private set; }
        public ThenNode(Node[] children, string source, int index, int length) : base(source, index, length) { Children = children; }
        private ThenNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node then" }._(Children.Select(ch => ch.ToHtml())); }
    }

    enum EscapeCode
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
        EndOfStringAlmost,    // \z
        EndOfStringReally,      // \Z
    }

    sealed class EscapeCodeNode : Node
    {
        public EscapeCode Code { get; private set; }
        public EscapeCodeNode(EscapeCode code, string source, int index, int length) : base(source, index, length) { Code = code; }
        private EscapeCodeNode() : base() { }
        public override object ToHtml() { return new SPAN { class_ = "node escapecode" }.Data("code", Code.ToString())._(Source); }
    }
}
