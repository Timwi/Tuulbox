using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace Tuulbox.Tools
{
    public sealed class JsonPrettify : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return true; } }
        string ITuul.Name { get { return "Prettify JSON"; } }
        string ITuul.UrlName { get { return "json"; } }
        string ITuul.Keywords { get { return "json prettify pretty format reformat readable browse browsable"; } }
        string ITuul.Description { get { return "Displays JSON in a readable, browsable way."; } }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            object html = null;
            string input = null;
            if (req.Method == HttpMethod.Post)
            {
                input = req.Post["input"].Value;
                try
                {
                    var parsed = JsonValue.Parse(input);
                    html = new DIV { id = "json-main" }._(
                        new H3("Browsable"),
                        new DIV { class_ = "controls" }._(
                            new DIV(
                                new A { href = "#", id = "json-expand-all", accesskey = "e" }._(Helpers.TextWithAccessKey("Expand all", "e")),
                                " | ",
                                new A { href = "#", id = "json-collapse-all", accesskey = "c" }._(Helpers.TextWithAccessKey("Collapse all", "c"))
                            )
                        ),
                        new JsonHtmlifier(parsed).Html,
                        new H3("Beautified"),
                        new PRE(JsonValue.ToStringIndented(parsed)));
                }
                catch (JsonParseException pe)
                {
                    html = new DIV { class_ = "error" }._(
                        new DIV { class_ = "input" }._(
                            new SPAN { class_ = "good" }._(input.Substring(0, pe.Index).ElideFront(20)),
                            new SPAN { class_ = "indicator" },
                            new SPAN { class_ = "bad" }._(input.Substring(pe.Index).ElideBack(20))
                        ),
                        pe.Message
                    );
                }
            }

            return new FORM { method = method.post, action = req.Url.ToFull() }._(
                html,
                new H3(Helpers.LabelWithAccessKey("JSON", "x", "json_json")),
                new DIV(new TEXTAREA { name = "input", id = "json_json", accesskey = "," }._(input)),
                new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Go for it", "g")))
            );
        }

        private sealed class JsonHtmlifier
        {
            private JsonValue _elem;
            private object _cache;
            private int _counter;

            public JsonHtmlifier(JsonValue elem)
            {
                _elem = elem;
                _counter = 0;
                _cache = null;
            }

            private object htmlify(JsonValue elem)
            {
                if (elem == null)
                    return new SPAN { class_ = "json_null" }._("null");
                else if (elem is JsonBool)
                    return new SPAN { class_ = "json_bool" }._(elem.ToString());
                else if (elem is JsonString)
                    return new SPAN { class_ = "json_string" }._(elem.ToString());
                else if (elem is JsonNumber)
                    return new SPAN { class_ = "json_number", title = elem.GetLongSafe().NullOr(i => "= 0x" + i.ToString("X")) }._(elem.ToString());

                var list = elem as JsonList;
                var dict = elem as JsonDict;
                Ut.Assert(list != null || dict != null);

                if (list != null && list.Count == 0)
                    return new SPAN { class_ = "json_list" }._(new SPAN { class_ = "brackets" }._("[ ]"));
                if (dict != null && dict.Count == 0)
                    return new SPAN { class_ = "json_dict" }._(new SPAN { class_ = "brackets" }._("{ }"));

                var c = _counter;
                _counter++;
                return new SPAN { class_ = list != null ? "json_list" : "json_dict" }.Data("c", c)._(
                    new SPAN { class_ = "brackets" }._(list != null ? "[" : "{"),
                    new SPAN { class_ = "collapsed", id = "c" + c }._(" ", new SPAN { class_ = "inner" }._("…"), " "),
                    new BLOCKQUOTE { id = "b" + c }._(list != null
                        ? list.Select((elm, i) => new DIV { class_ = "json_list_elem" }._(htmlify(elm), i < list.Count - 1 ? new SPAN { class_ = "comma" }._(",") : null))
                        : dict.Select((kvp, i) => new DIV { class_ = "json_dict_kvp" }._(new SPAN { class_ = "json_dict_key" }._(kvp.Key.JsEscape()), ": ", htmlify(kvp.Value), i < dict.Count - 1 ? new SPAN { class_ = "comma" }._(",") : null))),
                    new SPAN { class_ = "brackets" }._(list != null ? "]" : "}")
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
                                $('#b' + c).show();
                                $('#c' + c).hide();
                                $('#t' + c).text('−');
                            } else {
                                $('#c' + c).show();
                                $('#b' + c).hide();
                                $('#t' + c).text('+');
                            }
                        };

                        $('.json_list, .json_dict').each(function() {
                            var t = $(this);
                            var c = t.data('c');
                            all.push(c);
                            $('#b' + c).hide();
                            var o = $('#c' + c);
                            var btn = $(""<a href='#' class='button'>"").attr('id', 't' + c).text('+').insertBefore(o);
                            btn.click(function() {
                                on[c] = !(on[c] || false);
                                set(c);
                                return false;
                            });
                            set(c);
                        });

                        $('#json-expand-all, #json-collapse-all').click(function()
                        {
                            if ($(this).is('#json-collapse-all'))
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
                    #json-main { position: relative; }
                    .button { font-size: 9pt; border: 1px solid #888; border-radius: 1em; padding: 0 .5em; text-decoration: none; margin: 0 .4em; }
                    blockquote { margin: 0 0 0 1.5em; }
                    .brackets { font-weight: bold; font-size: 110%; }
                    .json_string, .json_dict_key { color: #0044ff; }
                    .json_null { color: #00dd22; font-weight: bold; }
                    .json_bool { color: #888800; font-weight: bold; }
                    .json_number { color: #ff4400; }
                    pre { border: 1px dashed #2288ff; background: #eef6ff; padding: .5em 1em; }
                ";
            }
        }
    }
}
