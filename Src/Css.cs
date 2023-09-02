using RT.Servers;
using RT.Util.ExtensionMethods;

namespace Tuulbox;

internal sealed class Css : ITuul
{
    private static readonly byte[] _css = Resources.MainCss;

    object ITuul.Handle(TuulboxModule module, HttpRequest req) => HttpResponse.Css(_css);

    bool ITuul.Enabled => true;
    bool ITuul.Listed => false;
    string ITuul.Name => null;
    string ITuul.UrlName => "css";
    string ITuul.Keywords => null;
    string ITuul.Description => null;
    string ITuul.Js => null;
    string ITuul.Css => null;
}
