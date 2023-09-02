using System.Linq;
using RT.Servers;
using RT.TagSoup;

namespace Tuulbox.Tools;

internal sealed class ListAllTools : ITuul
{
    bool ITuul.Enabled => true;
    bool ITuul.Listed => false;
    string ITuul.Name => "List of all tuuls";
    string ITuul.UrlName => null;
    string ITuul.Keywords => "complete list all tuuls";
    string ITuul.Description => "Shows a list of all the tuuls available in the Tuulbox.";
    string ITuul.Js => null;
    string ITuul.Css => null;

    object ITuul.Handle(TuulboxModule module, HttpRequest req) => new UL(
            TuulboxModule.Tuuls.Where(tuul => tuul.Listed).OrderBy(tuul => tuul.Name).Select(tuul => new LI(
                new DIV { class_ = "tuulname" }._(new A { href = req.Url.WithParents(2, tuul.UrlName, module.Settings.UseDomain).ToFull() }._(tuul.Name)),
                new DIV { class_ = "explain" }._(tuul.Description)
            ))
        );
}
