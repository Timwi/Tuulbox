using System.Linq;
using RT.Json;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Tuulbox.Tools
{
    public sealed class JsonPrettify : PrettifierBase<JsonValue>
    {
        protected override string ituulName { get { return "Prettify JSON"; } }
        protected override string ituulUrlName { get { return "json"; } }
        protected override string ituulKeywords { get { return "json prettify pretty format reformat readable browse browsable"; } }
        protected override string ituulDescription { get { return "Displays JSON in a readable, browsable way."; } }

        protected override JsonValue Parse(string input) { return JsonValue.Parse(input); }
        protected override object Textify(JsonValue parsed) { return JsonValue.ToStringIndented(parsed); }
        protected override object Htmlify(JsonValue parsed) { return new JsonHtmlifier(parsed).Html; }

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
                    return new SPAN { class_ = "json_empty" }._(new SPAN { class_ = "json_brackets" }._("[ ]"));
                if (dict != null && dict.Count == 0)
                    return new SPAN { class_ = "json_empty" }._(new SPAN { class_ = "json_brackets" }._("{ }"));

                var c = _counter;
                _counter++;
                return new SPAN { class_ = "json_expandable beau_expandable" }.Data("c", c)._(
                    new SPAN { class_ = "json_brackets" }._(list != null ? "[" : "{"),
                    new SPAN { id = "c" + c }._(" ... "),
                    new BLOCKQUOTE { id = "b" + c }._(list != null
                        ? list.Select((elm, i) => new DIV { class_ = "json_list_elem" }._(htmlify(elm), i < list.Count - 1 ? new SPAN { class_ = "comma" }._(",") : null))
                        : dict.Select((kvp, i) => new DIV { class_ = "json_dict_kvp" }._(new SPAN { class_ = "json_dict_key" }._(kvp.Key.JsEscape()), ": ", htmlify(kvp.Value), i < dict.Count - 1 ? new SPAN { class_ = "comma" }._(",") : null))),
                    new SPAN { class_ = "json_brackets" }._(list != null ? "]" : "}")
                );
            }

            public object Html { get { return _cache ?? (_cache = htmlify(_elem)); } }
        }

        protected override string ExtraCss
        {
            get
            {
                return @"
                    .json_expandable { position: relative; }
                    .json_dict_kvp > .json_expandable { margin-left: 2em; }
                    .json_brackets { font-weight: bold; font-size: 110%; }
                    .json_string, .json_dict_key { color: #0044ff; }
                    .json_null { color: #00dd22; font-weight: bold; }
                    .json_bool { color: #888800; font-weight: bold; }
                    .json_number { color: #ff4400; }
                ";
            }
        }
    }
}
