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
    public sealed class XmlPrettify : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return true; } }
        string ITuul.Name { get { return "Prettify XML"; } }
        string ITuul.UrlName { get { return "xml"; } }
        string ITuul.Keywords { get { return "xml prettify pretty format reformat readable browse browsable"; } }
        string ITuul.Description { get { return "Displays XML in a readable, browsable way."; } }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            if (req.Method == HttpMethod.Post)
            {
                try
                {
                    var rootElement = XDocument.Parse(req.Post["input"].Value).Root;
                    return new DIV { id = "xml-main" }._(
                        new H3("Browsable"),
                        new DIV { class_ = "controls" }._(
                            new DIV(
                                new A { href = "#", id = "xml-expand-all", accesskey = "e" }._(Helpers.TextWithAccessKey("Expand all", "e")),
                                " | ",
                                new A { href = "#", id = "xml-collapse-all", accesskey = "c" }._(Helpers.TextWithAccessKey("Collapse all", "c"))
                            )
                        ),
                        new XmlHtmlifier(rootElement).Html,
                        new H3("Beautified"),
                        new PRE(new Func<object>(() => rootElement.ToString())));
                }
                catch (Exception e)
                {
                    return Ut.NewArray<object>(
                        new H3("Error"),
                        new P(e.Message)
                    );
                }
            }
            else
            {
                return new FORM { method = method.post, action = req.Url.ToFull() }._(
                    new H3(Helpers.LabelWithAccessKey("XML", "x", "xml_xml")),
                    new DIV(new TEXTAREA { name = "input", id = "xml_xml", accesskey = "," }._()),
                    new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Go for it", "g")))
                );
            }
        }

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

                yield return new DIV { class_ = "tag" }.Data("c", c)._(
                    new SPAN { class_ = "anglebrackets" }._("<"),
                    new SPAN { class_ = "tagname" }._(stringifyXName(elem.Name)),
                    !elem.HasAttributes ? null : Ut.NewArray<object>(
                        " ",
                        new TABLE { class_ = "attributes" }._(
                            elem.Attributes().Select(attr => new TR(
                                new TH { class_ = "attributename" }._(stringifyXName(attr.Name)),
                                new TD { class_ = "equals" }._("="),
                                new TD { class_ = "attributevalue" }._(attr.Value)
                            ))
                        )
                    ),
                    new SPAN { class_ = "anglebrackets" }._(anyContent ? ">" : "/>"),
                    trivialContent
                        ? (object) new SPAN { class_ = "trivial text" }._(trivialText)
                        : anyContent
                            ? Ut.NewArray<object>(
                                new SPAN { class_ = "tagcollapsed", id = "o" + c }._(" ... "),
                                new BLOCKQUOTE { class_ = "tagcontents", id = "c" + c, style = "display:none" }._(elem.Nodes().Select(node => node.IfType(
                                    (XElement subElem) => htmlify(subElem),
                                    (XText text) =>
                                    {
                                        var newValue = text.Value.RemoveCommonIndentation().TrimStart('\r', '\n').TrimEnd();
                                        text.Value = newValue;
                                        return new SPAN { class_ = "text" }._(newValue);
                                    },
                                    (XAttribute attr) => (object) null,
                                    @elseObj => null))))
                            : null,
                    anyContent ? Ut.NewArray(
                        new SPAN { class_ = "anglebrackets" }._("</"),
                        new SPAN { class_ = "tagname" }._(stringifyXName(elem.Name)),
                        new SPAN { class_ = "anglebrackets" }._(">")
                    ) : null
                );
            }

            public object Html { get { return _cache ?? (_cache = htmlify(_elem)); } }
        }

        string ITuul.Js
        {
            get
            {
                return @"
                    $(function() {
                        var on = { '0': true };
                        var all = [];
                        function set(c) {
                            if (on[c]) {
                                $('#c' + c).show();
                                $('#o' + c).hide();
                                $('#t' + c).text('−');
                            } else {
                                $('#o' + c).show();
                                $('#c' + c).hide();
                                $('#t' + c).text('+');
                            }
                        };

                        $('.tag').each(function() {
                            var t = $(this);
                            var c = t.data('c');
                            if (c === void(0))
                                return;
                            all.push(c);
                            $('#c' + c).hide();
                            var o = $('#o' + c);
                            var btn = $(""<a href='#' class='button'>"").attr('id', 't' + c).text('+').prependTo(t);
                            btn.click(function() {
                                on[c] = !(on[c] || false);
                                set(c);
                                return false;
                            });
                            set(c);
                        });

                        $('#xml-expand-all, #xml-collapse-all').click(function()
                        {
                            if ($(this).is('#xml-collapse-all'))
                                on = {};
                            else
                                for (var i = 0; i < all.length; i++)
                                    on[i] = true;
                            for (var i = 0; i < all.length; i++)
                                set(all[i]);
                            return false;
                        });
                    });
                ";
            }
        }

        string ITuul.Css
        {
            get
            {
                return @"
                    #xml-main { position: relative; }
                    .tag { font-size: 15pt; margin: .2em 0; position: relative; white-space: nowrap; }
                    .tagname { color: #00f; }
                    .attributes { display: inline-table; vertical-align: top; font-size: 12pt; }
                    .button { font-size: 9pt; border: 1px solid #888; border-radius: 1em; padding: 0 .5em; text-decoration: none; position: absolute; right: 100%; top: .4em; margin-right: .7em; }
                    .attributes td, .attributes th { vertical-align: top; padding: 0; }
                    .attributes th.attributename { font-weight: bold; color: #080; text-align: right; }
                    .attributes td.equals { padding: 0 .1em; }
                    .attributes td.attributevalue { font-style: italic; color: #880; }
                    blockquote { margin: 0 0 0 1.5em; }
                    .text { white-space: pre-line; }
                    .trivial { padding: 0 .25em; }
                    pre { border: 1px dashed #2288ff; background: #eef6ff; padding: .5em 1em; }
                ";
            }
        }
    }
}
