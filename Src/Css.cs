using RT.Servers;
using RT.Util.ExtensionMethods;

namespace Tuulbox
{
    sealed class Css : ITool
    {
        private static byte[] _css = @"

body {
    background: #eee;
    margin: -10px 0 0 0;
    font-family: ""Candara"", ""Calibri"", ""Tahoma"", ""Verdana"", ""Arial"", sans-serif;
}
.everything {
    max-width: 50em;
    margin: 0 auto 20px;
    background: white;
    box-shadow: 0 0 5px rgba(0, 0, 0, .5);
    padding: 3em;
    border-radius: 7px;
}
h1 {
    font-variant: small-caps;
    font-size: 47pt;
}
div.search {
    float: right;
}
.content {
    border-top: 1px solid #ccc;
    padding-top: 2em;
}
.content h2 {
    font-size: 24pt;
    font-variant: small-caps;
}
textarea {
    width: 100%;
    height: 15em;
}
.toolname { font-weight: bold; }
.explain { color: #888; margin: .2em 0 .7em 2em; font-size: 80%; }

table.layout { table-layout: fixed; width: 100%; border-collapse: collapse; }
table.layout col { width: 49%; }
table.layout col.spacer { width: 2%; }
table.layout td { vertical-align: top; }

".ToUtf8();

        object ITool.Handle(HttpRequest req)
        {
            return HttpResponse.Css(_css);
        }

        string ITool.Name { get { return null; } }
        string ITool.Url { get { return "/css"; } }
        string ITool.Keywords { get { return null; } }
        string ITool.Description { get { return null; } }
        string ITool.Js { get { return null; } }
        string ITool.Css { get { return null; } }
    }
}
