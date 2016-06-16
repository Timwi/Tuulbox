using System;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace Tuulbox.Tools
{
    public abstract class PrettifierBase<TParsed> : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return true; } }
        string ITuul.Name { get { return ituulName; } }
        string ITuul.UrlName { get { return ituulUrlName; } }
        string ITuul.Keywords { get { return ituulKeywords; } }
        string ITuul.Description { get { return ituulDescription; } }
        protected abstract string ituulName { get; }
        protected abstract string ituulUrlName { get; }
        protected abstract string ituulKeywords { get; }
        protected abstract string ituulDescription { get; }

        protected abstract TParsed Parse(string input);
        protected abstract object Textify(TParsed parsed);
        protected abstract object Htmlify(TParsed parsed);

        protected virtual string ExtraJs { get { return ""; } }
        protected virtual string ExtraCss { get { return ""; } }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            object html = null;
            string input = null;
            if (req.Method == HttpMethod.Post)
            {
                input = req.Post["input"].Value;
                try
                {
                    var parsed = Parse(input);
                    html = new DIV { id = "beau-main" }._(
                        new H3("Browsable"),
                        new DIV { class_ = "controls" }._(
                            new DIV(
                                new A { href = "#", id = "beau-expand-all", accesskey = "e" }._(Helpers.TextWithAccessKey("Expand all", "e")),
                                " | ",
                                new A { href = "#", id = "beau-collapse-all", accesskey = "o" }._(Helpers.TextWithAccessKey("Collapse all", "o"))
                            )
                        ),
                        Htmlify(parsed),
                        new H3("Beautified"),
                        new DIV { class_ = "beau_textarea" }._(
                            new TEXTAREA { id = "beau_text" }._(Textify(parsed)),
                            new BUTTON { class_ = "beau_copy", accesskey = "c", onclick = "var t=$('#beau_text')[0];t.focus();t.setSelectionRange(0,t.value.length);document.execCommand('copy');", type = btype.button }._(Helpers.TextWithAccessKey("Copy", "c"))
                        )
                    );
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
                catch (Exception e)
                {
                    html = Ut.NewArray<object>(
                        new H3("Error"),
                        new P(e.Message)
                    );
                }
            }

            return new FORM { method = method.post, action = req.Url.ToFull() }._(
html,
                new H3(Helpers.LabelWithAccessKey("Source", "s", "beau_beau")),
                new DIV(new TEXTAREA { name = "input", id = "beau_beau", accesskey = "," }._(input)),
                new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Go for it", "g")))
            );
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

                        $('.beau_expandable').each(function() {
                            var t = $(this);
                            var c = t.data('c');
                            all.push(c);
                            $('#b' + c).hide();
                            var o = $('#c' + c);
                            var btn = $(""<a href='#' class='beau_button'>"").attr('id', 't' + c).text('+').insertBefore(o);
                            btn.click(function() {
                                on[c] = !(on[c] || false);
                                set(c);
                                return false;
                            });
                            set(c);
                        });

                        $('#beau-expand-all, #beau-collapse-all').click(function()
                        {
                            if ($(this).is('#beau-collapse-all'))
                                on = {};
                            else
                                for (var i = 0; i < all.length; i++)
                                    on[i] = true;
                            for (var i = 0; i < all.length; i++)
                                set(all[i]);
                            return false;
                        });
                    });
                " + ExtraJs;
            }
        }

        string ITuul.Css
        {
            get
            {
                return @"
                    #beau-main { position: relative; }
                    .beau_button { font-size: 9pt; border: 1px solid #888; border-radius: 1em; padding: 0 .5em; text-decoration: none; margin: 0 .4em; }
                    .beau_button { position: absolute; right: 100%; top: .4em; margin-right: .7em; }
                    blockquote { margin: 0 0 0 1.5em; }
                    .beau_textarea { position: relative; }
                    .beau_copy { position: absolute; bottom: 100%; right: 0; }
                " + ExtraCss;
            }
        }
    }
}
