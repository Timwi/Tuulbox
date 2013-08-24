using RT.Servers;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace Tuulbox
{
    sealed class Js : ITool
    {
        private static byte[] _js = JsonValue.Fmt(@"
// No site-global JavaScript yet!
").ToUtf8();

        object ITool.Handle(HttpRequest req)
        {
            return HttpResponse.JavaScript(_js);
        }

        string ITool.Name { get { return null; } }
        string ITool.Url { get { return "/js"; } }
        string ITool.Keywords { get { return null; } }
        string ITool.Description { get { return null; } }
        string ITool.Js { get { return null; } }
        string ITool.Css { get { return null; } }
    }
}
