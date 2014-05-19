using RT.Servers;

namespace Tuulbox
{
    public interface ITuul
    {
        bool Enabled { get; }
        string Name { get; }
        string UrlName { get; }
        string Keywords { get; }
        string Description { get; }
        object Handle(TuulboxModule module, HttpRequest req);
        string Js { get; }
        string Css { get; }
    }
}
