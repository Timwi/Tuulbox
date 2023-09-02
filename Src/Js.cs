using RT.Servers;

namespace Tuulbox;

internal sealed class Js : ITuul
{
    private static readonly byte[] _js = Resources.MainJs;

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
