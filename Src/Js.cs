using RT.Json;
using RT.Servers;
using RT.Util.ExtensionMethods;

namespace Tuulbox;

internal sealed class Js : ITuul
{
    private static readonly byte[] _js = JsonValue.Fmt(Resources.MainJs).ToUtf8();

    object ITuul.Handle(TuulboxModule module, HttpRequest req) => HttpResponse.JavaScript(_js);

    bool ITuul.Enabled => true;
    bool ITuul.Listed => false;
    string ITuul.Name => null;
    string ITuul.UrlName => "js";
    string ITuul.Keywords => null;
    string ITuul.Description => null;
    string ITuul.Js => null;
    string ITuul.Css => null;
}
