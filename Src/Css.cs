using RT.Servers;
using RT.Util.ExtensionMethods;

namespace Tuulbox
{
    sealed class Css : ITuul
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
.tuulname { font-weight: bold; }
.explain { color: #888; margin: .2em 0 .7em 2em; font-size: 80%; }

table.layout { table-layout: fixed; width: 100%; border-collapse: collapse; }
table.layout th, table.layout td { vertical-align: top; }

kbd.accesskey { font-family: inherit; text-decoration: underline; }
".ToUtf8();

        object ITuul.Handle(HttpRequest req)
        {
            return HttpResponse.Css(_css);
        }

        bool ITuul.Enabled { get { return true; } }
        string ITuul.Name { get { return null; } }
        string ITuul.UrlName { get { return "css"; } }
        string ITuul.Keywords { get { return null; } }
        string ITuul.Description { get { return null; } }
        string ITuul.Js { get { return null; } }
        string ITuul.Css { get { return null; } }
    }
}
