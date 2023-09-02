using System;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util.ExtensionMethods;

namespace Tuulbox.Tools;

internal sealed class RankingComparison : ITuul
{
    bool ITuul.Enabled => true;
    bool ITuul.Listed => true;
    string ITuul.Name => "Rank compare";
    string ITuul.UrlName => "rankcompare";
    string ITuul.Keywords => "list ranking compare comparison visualize visualization picture graphic image boxes curves red yellow green";
    string ITuul.Description => "Compares two lists that represent rankings of things (for example, episodes of a show ranked by personal preference; games ranked by difficulty; etc.) and shows the similarities and differences visually.";
    string ITuul.Js => null;
    string ITuul.Css => null;

    object ITuul.Handle(TuulboxModule module, HttpRequest req)
    {
        if (req.Method == HttpMethod.Post)
        {
            if (!req.Post.ContainsKey("first") || !req.Post.ContainsKey("second"))
                throw new HttpException(HttpStatusCode._400_BadRequest, userMessage: "No lists provided.");

            var first = req.Post["first"].Value.Replace("\r", "").Split('\n');
            var second = req.Post["second"].Value.Replace("\r", "").Split('\n');
            var maxDistance = first.Intersect(second, StringComparer.InvariantCultureIgnoreCase).Max(epi => Math.Abs(first.IndexOf(epi) - second.IndexOf(epi)));
            double hue(string element)
            {
                var tIx = first.IndexOf(element, StringComparer.InvariantCultureIgnoreCase);
                var hIx = second.IndexOf(element, StringComparer.InvariantCultureIgnoreCase);
                return tIx == -1 || hIx == -1 || maxDistance == 0 ? 200 : 120 - 120.0 / maxDistance * Math.Abs(tIx - hIx);
            }

            var firstBoxes = first.Select((epi, ix) => $@"<rect x='0' y='{ix}' width='10' height='.9' fill='hsl({hue(epi)}, 90%, 60%)' stroke='black' stroke-width='.01' /><text x='.25' y='{ix + .65}' font-size='.5'>{ix + 1}. {epi.HtmlEscape()}</text>").JoinString();
            var secondBoxes = second.Select((epi, ix) => $@"<rect x='0' y='{ix}' width='10' height='.9' fill='hsl({hue(epi)}, 90%, 60%)' stroke='black' stroke-width='.01' /><text x='.25' y='{ix + .65}' font-size='.5'>{ix + 1}. {epi.HtmlEscape()}</text>").JoinString();
            var lines = first.Select((epi, tIx) =>
            {
                var hIx = second.IndexOf(epi, StringComparer.InvariantCultureIgnoreCase);
                return hIx == -1
                    ? null
                    : $"<path d='M10,{tIx + .45} C13,{tIx + .45} 17,{hIx + .45} 20,{hIx + .45}' stroke-width='.05' stroke='hsl({hue(epi)}, 90%, 60%)' fill='none' />";
            }).JoinString();

            return HttpResponse.Html($@"<!DOCTYPE html>
<html>
<head>
    <title>{req.Post["title"].Value}</title>
</head>
<body>
    <svg xmlns='http://www.w3.org/2000/svg' viewBox='-.1 -1 30.2 {Math.Max(first.Length, second.Length) + 2}' style='width: 20cm; margin: 0 auto; display: block;'>
        <text x='.25' y='-.3' font-size='.75'>{req.Post["header1"].Value}</text>
        <text x='20.25' y='-.3' font-size='.75'>{req.Post["header2"].Value}</text>
        {firstBoxes}
        <g transform='translate(20)'>{secondBoxes}</g>
        {lines}
    </svg>
</body>
</html>");
        }
        else
        {
            return new FORM { action = req.Url.ToFull(), enctype = enctype.multipart_formData, method = method.post }._(
                new DIV("Header for first column: ", new INPUT { type = itype.text, name = "header1" }),
                new DIV("First list: (newline-separated; will form the left column)"),
                new TEXTAREA { name = "first" },
                new DIV("Header for second column: ", new INPUT { type = itype.text, name = "header2" }),
                new DIV("Second list: (newline-separated; will form the right column; items must match items in first list precisely)"),
                new TEXTAREA { name = "second" },
                new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Generate", "g"))),
                new DIV("After generating, press Ctrl+S to save the page to your disk.")
            );
        }
    }
}
