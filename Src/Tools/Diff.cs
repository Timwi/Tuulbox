using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace Tuulbox.Tools
{
    public sealed class Diff : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return true; } }
        string ITuul.Name { get { return "Diff"; } }
        string ITuul.UrlName { get { return "diff"; } }
        string ITuul.Keywords { get { return "diff difftool difference textdiff worddiff code changes"; } }
        string ITuul.Description { get { return "Visualizes the differences between two provided chunks of text (e.g. source code)."; } }
        string ITuul.Js
        {
            get
            {
                return @"
                    $(function()
                    {
                        $('#diff-font-monospace').click(function()
                        {
                            $('.diff-pre').css('font-family', 'monospace');
                            return false;
                        });
                        $('#diff-font-proportional').click(function()
                        {
                            $('.diff-pre').css('font-family', 'inherit');
                            return false;
                        });
                        $('#diff-wordwrap').click(function()
                        {
                            $('.diff-pre').css('white-space', this.checked ? 'pre-wrap' : 'pre');
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
                    .diff-Ins { background: #8f8; font-weight: bold; }
                    .diff-Del { background: #f88; font-style: italic; }
                    .diff-tab-control { position: relative; }
                    .diff-pre { display: inline-block; min-width: 100%; }
                ";
            }
        }

        static Regex _interestingLine = new Regex(@"\S\s*\S", RegexOptions.Singleline | RegexOptions.Compiled);
        static Regex _interestingWord = new Regex(@"^\w", RegexOptions.Singleline | RegexOptions.Compiled);

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            string inputOld = null;
            string inputNew = null;
            object output = null;

            if (req.Method == HttpMethod.Post)
            {
                inputOld = req.Post["old"].Value;
                inputNew = req.Post["new"].Value;
                if (inputOld.Length + inputNew.Length < 2048)
                {
                    byte[] gz;
                    using (var m = new MemoryStream())
                    {
                        using (var z = new GZipOutputStream(m))
                        {
                            z.SetLevel(9);
                            z.Write(new JsonList { inputOld, inputNew }.ToString().ToUtf8());
                        }
                        gz = m.ToArray();
                    }
                    return HttpResponse.Redirect(req.Url.WithQuery("d", gz.Base64UrlEncode()));
                }
            }
            else if (req.Url["d"] != null)
            {
                try
                {
                    JsonList d = null;
                    using (var m = new MemoryStream(req.Url["d"].Base64UrlDecode()))
                    using (var z = new GZipInputStream(m))
                        d = JsonList.Parse(z.ReadAllText());
                    if (d != null && d.Count == 2)
                    {
                        inputOld = d[0].GetString();
                        inputNew = d[1].GetString();
                    }
                }
                catch
                {
                }
            }

            if (inputOld != null && inputNew != null)
            {
                inputOld = inputOld.Replace("\r", "");
                inputNew = inputNew.Replace("\r", "");

                var lineDiff = Ut.Diff(inputOld.Split('\n'), inputNew.Split('\n'));

                var wordDiff = Ut.Diff(
                    old: Regex.Split(inputOld, "$", RegexOptions.Multiline),
                    @new: Regex.Split(inputNew, "$", RegexOptions.Multiline),
                    predicate: _interestingLine.IsMatch,
                    postProcessor: (ol, nw) => Ut.Diff(
                        old: Regex.Split(ol.JoinString(), @"\b"),
                        @new: Regex.Split(nw.JoinString(), @"\b"),
                        predicate: _interestingWord.IsMatch,
                        postProcessor: (ol2, nw2) => Ut.Diff(
                            old: ol2.SelectMany(o => Regex.IsMatch(o, @"^\w") ? new[] { o } : o.Split(1)),
                            @new: nw2.SelectMany(n => Regex.IsMatch(n, @"^\w") ? new[] { n } : n.Split(1)),
                            predicate: str => !string.IsNullOrWhiteSpace(str)
                        )
                    )
                );

                output = new DIV { class_ = "tab-control diff-tab-control" }._(
                    new DIV { class_ = "controls" }._(
                        new DIV(
                            new B("Font:"),
                            " ",
                            new A { href = "#", id = "diff-font-monospace", accesskey = "m" }._(Helpers.TextWithAccessKey("monospace", "m")),
                            " | ",
                            new A { href = "#", id = "diff-font-proportional", accesskey = "p" }._(Helpers.TextWithAccessKey("proportional", "p"))
                        ),
                        new DIV(
                            new INPUT { type = itype.checkbox, id = "diff-wordwrap" }, " ", Helpers.LabelWithAccessKey("allow word-wrapping", "r", "diff-wordwrap")
                        )
                    ),
                    new DIV { class_ = "tabs" }._(
                        new A { href = "#", class_ = "selected", accesskey = "l" }.Data("tab", "diff-tab-line")._(Helpers.TextWithAccessKey("Line diff", "l")),
                        new A { href = "#", accesskey = "w" }.Data("tab", "diff-tab-word")._(Helpers.TextWithAccessKey("Word diff", "w"))
                    ),
                    new DIV { class_ = "tab", id = "diff-tab-line" }._(
                        new PRE { class_ = "diff-pre" }._(lineDiff.Select(line => new DIV { class_ = "diff-" + line.Item2 }._(line.Item1.Length == 0 ? "\n" : line.Item1)))
                    ),
                    new DIV { class_ = "tab", id = "diff-tab-word" }._(
                        new PRE { class_ = "diff-pre" }._(wordDiff.GroupConsecutiveBy(tup => tup.Item2).Select(group => new SPAN { class_ = "diff-" + group.Key }._(group.Select(tup => tup.Item1))))
                    )
                );
            }

            return Ut.NewArray<object>(
                output.NullOr(o => new object[] { new H3("Result"), o }),

                new FORM { action = req.Url.WithoutQuery("d").ToFull(), method = method.post }._(
                    new H3("Old:"),
                    new DIV(new TEXTAREA { name = "old", accesskey = "," }._(inputOld)),
                    new H3("New:"),
                    new DIV(new TEXTAREA { name = "new", accesskey = "." }._(inputNew)),
                    new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Go for it", "g")))
                )
            );
        }
    }
}
