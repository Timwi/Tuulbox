using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;

namespace Tuulbox.Tools
{
    public sealed class XmlPrettify : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        string ITuul.Name { get { return "Prettify XML"; } }
        string ITuul.UrlName { get { return "xml"; } }
        string ITuul.Keywords { get { return "xml prettify pretty format reformat readable"; } }
        string ITuul.Description { get { return "Displays XML in a more readable way."; } }

        object ITuul.Handle(HttpRequest req)
        {
            if (req.Method == HttpMethod.Post)
            {
                try
                {
                    return new XmlHtmlifier(XDocument.Parse(req.Post["input"].Value).Root).Html;
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
                    new DIV(new TEXTAREA { name = "input", id = "xml_xml" }._()),
                    new DIV(new BUTTON { type = btype.submit, accesskey = "s" }._(Helpers.TextWithAccessKey("Submit", "s")))
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
                var c = _counter;
                _counter++;
                yield return new DIV { class_ = "tag" }.Data("c", c)._(
                    new SPAN { class_ = "anglebrackets" }._("<"),
                    new SPAN { class_ = "tagname" }._(stringifyXName(elem.Name)),
                    !elem.HasAttributes ? null : Ut.NewArray<object>(
                        " ",
                        new TABLE { class_ = "attributes" }._(
                            elem.Attributes().Select(attr => new TR(
                                new TH { class_ = "attributename" }._(stringifyXName(attr.Name)),
                                new TD { class_ = "attributevalue" }._(attr.Value)
                            ))
                        )
                    ),
                    new SPAN { class_ = "anglebrackets" }._(">")
                );
                if (elem.Nodes().Any())
                    yield return new BLOCKQUOTE { class_ = "tagcontents", id = "c" + c }._(elem.Nodes().Select(node => node.IfType(
                        (XElement subElem) => htmlify(subElem),
                        (XText text) => (object) new SPAN { class_ = "text" }._(text),
                        (XAttribute attr) => null,
                        @elseObj => null)));
            }

            public object Html { get { return _cache ?? (_cache = htmlify(_elem)); } }
        }

        string ITuul.Js
        {
            get
            {
                return @"
                    $(function() {
                        var open = window.location.hash.substr(1).split(',').reduce(function(p, n) { p[n] = true; return p; }, {});
                        $('.tag').each(function() {
                            var t = $(this);
                            var c = t.data('c');
                            var i = $('#c' + c);
                            if (i.length) {
                                i.hide();
                                var btn = $(""<a href='#' class='button'>"").text('+');
                                var on = open[c] || false;
                                var set = function() {
                                    if (on) {
                                        i.show();
                                        btn.text('−');
                                        open[c] = true;
                                    } else {
                                        i.hide();
                                        btn.text('+');
                                        delete open[c];
                                    }
                                };
                                btn.click(function() {
                                    on = !on;
                                    set();
                                    window.location.hash = '#' + Object.keys(open).join(',');
                                    return false;
                                });
                                set();
                                t.prepend(btn);
                            }
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
                    .tag { font-size: 15pt; background: #eef6ff; margin: .2em 0; position: relative; }
                    .tagname { color: #00f; }
                    .attributes { display: inline-table; vertical-align: top; font-size: 12pt; }
                    .button { font-size: 9pt; border: 1px solid #888; border-radius: 1em; padding: 0 .5em; text-decoration: none; position: absolute; right: 100%; top: .4em; margin-right: .7em; }
                    .attributes td, .attributes th { vertical-align: top; padding: 0; }
                    .attributes th.attributename { font-weight: bold; color: #080; text-align: right; padding: 0 .7em; }
                    .attributes td.attributevalue { font-style: italic; color: #880; }
                    blockquote { margin: 0 0 0 3em; }
                    .text { background: #ddeeff; padding: .1em .3em; border: 1px solid #bbddff; white-space: pre; }
                ";
            }
        }
    }
}
