using RT.Servers;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace Tuulbox
{
    sealed class Js : ITuul
    {
        private static byte[] _js = JsonValue.Fmt(@"
// No site-global JavaScript yet!
").ToUtf8();

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            return HttpResponse.JavaScript(_js);
        }

        bool ITuul.Enabled { get { return true; } }
        string ITuul.Name { get { return null; } }
        string ITuul.UrlName { get { return "js"; } }
        string ITuul.Keywords { get { return null; } }
        string ITuul.Description { get { return null; } }
        string ITuul.Js { get { return null; } }
        string ITuul.Css { get { return null; } }
    }
}
