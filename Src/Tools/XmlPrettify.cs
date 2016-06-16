using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Tuulbox.Tools
{
    public sealed class XmlPrettify : PrettifierBase<XElement>
    {
        protected override string ituulName { get { return "Prettify XML"; } }
        protected override string ituulUrlName { get { return "xml"; } }
        protected override string ituulKeywords { get { return "xml prettify pretty format reformat readable browse browsable"; } }
        protected override string ituulDescription { get { return "Displays XML in a readable, browsable way."; } }

        protected override XElement Parse(string input) { return XDocument.Parse(input).Root; }
        protected override object Htmlify(XElement parsed) { return new XmlHtmlifier(parsed).Html; }
        protected override object Textify(XElement parsed) { return new Func<object>(parsed.ToString); }

        private sealed class XmlHtmlifier
        {
            private XElement _elem;
            private object _cache;
            private int _counter;

            public XmlHtmlifier(XElement elem)
            {
                _elem = elem;
                _counter = 0;
                _cache = null;
            }

            private object stringifyXName(XName name)
            {
                return name.LocalName;
            }

            private IEnumerable<object> htmlify(XElement elem)
            {
                var anyContent = elem.Nodes().Any();
                var trivialContent = false;
                var firstTwoNodes = elem.Nodes().Take(2).ToArray();
                string trivialText = null;
                if (firstTwoNodes.Length == 1 && firstTwoNodes[0] is XText)
                {
                    trivialText = ((XText) firstTwoNodes[0]).Value;
                    if (Regex.IsMatch(trivialText, @"^[ \r\n]*([^\r\n]*)[ \r\n]*$", RegexOptions.Singleline))
                    {
                        trivialContent = true;
                        trivialText = trivialText.Trim();
                        ((XText) firstTwoNodes[0]).Value = trivialText;
                    }
                }
                int? c = null;
                if (anyContent && !trivialContent)
                    c = _counter++;

                yield return new DIV { class_ = "xml_tag beau_expandable" }.Data("c", c)._(
                    new SPAN { class_ = "xml_brackets" }._("<"),
                    new SPAN { class_ = "xml_tagname" }._(stringifyXName(elem.Name)),
                    !elem.HasAttributes ? null : Ut.NewArray<object>(
                        " ",
                        new TABLE { class_ = "xml_attributes" }._(
                            elem.Attributes().Select(attr => new TR(
                                new TH { class_ = "xml_attributename" }._(stringifyXName(attr.Name)),
                                new TD { class_ = "xml_equals" }._("="),
                                new TD { class_ = "xml_attributevalue" }._(attr.Value)
                            ))
                        )
                    ),
                    new SPAN { class_ = "xml_brackets" }._(anyContent ? ">" : "/>"),
                    trivialContent
                        ? (object) new SPAN { class_ = "xml_trivial xml_text" }._(trivialText)
                        : anyContent
                            ? Ut.NewArray<object>(
                                new SPAN { id = "c" + c }._(" ... "),
                                new BLOCKQUOTE { id = "b" + c }._(elem.Nodes().Select(node => node.IfType(
                                    (XElement subElem) => htmlify(subElem),
                                    (XText text) =>
                                    {
                                        var newValue = text.Value.RemoveCommonIndentation().TrimStart('\r', '\n').TrimEnd();
                                        text.Value = newValue;
                                        return new SPAN { class_ = "xml_text" }._(newValue);
                                    },
                                    (XAttribute attr) => (object) null,
                                    @elseObj => null))))
                            : null,
                    anyContent ? Ut.NewArray(
                        new SPAN { class_ = "xml_brackets" }._("</"),
                        new SPAN { class_ = "xml_tagname" }._(stringifyXName(elem.Name)),
                        new SPAN { class_ = "xml_brackets" }._(">")
                    ) : null
                );
            }

            public object Html { get { return _cache ?? (_cache = htmlify(_elem)); } }
        }

        protected override string ExtraCss
        {
            get
            {
                return @"
                    .xml_tag { font-size: 15pt; margin: .2em 0; position: relative; white-space: nowrap; }
                    .xml_tagname { color: #00f; }
                    .xml_text { white-space: pre-line; }
                    .xml_trivial { padding: 0 .25em; }
                    .xml_attributes { display: inline-table; vertical-align: top; font-size: 12pt; }
                    .xml_attributes td, .xml_attributes th { vertical-align: top; padding: 0; }
                    .xml_attributes th.xml_attributename { font-weight: bold; color: #080; text-align: right; }
                    .xml_attributes td.xml_equals { padding: 0 .1em; }
                    .xml_attributes td.xml_attributevalue { font-style: italic; color: #880; }
                    .xml_brackets { font-size: 90%; }
                    .beau_button { position: absolute; right: 100%; top: .4em; margin-right: .7em; }
                ";
            }
        }
    }
}
