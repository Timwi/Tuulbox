using RT.Servers;

namespace Tuulbox
{
    interface ITool
    {
        string Name { get; }
        string Url { get; }
        string Keywords { get; }
        string Description { get; }
        object Handle(HttpRequest req);
        string Js { get; }
        string Css { get; }
    }
}
