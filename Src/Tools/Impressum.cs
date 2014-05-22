using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Servers;
using RT.TagSoup;
using RT.Util.ExtensionMethods;

namespace Tuulbox.Tools
{
    public sealed class Impressum : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return false; } }
        string ITuul.Name { get { return "Impressum"; } }
        string ITuul.UrlName { get { return "impressum"; } }
        string ITuul.Keywords { get { return ""; } }
        string ITuul.Description { get { return ""; } }
        string ITuul.Js { get { return null; } }
        string ITuul.Css { get { return null; } }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            return new P(
                module.Settings.Impressum
                    .Select(line =>
                    {
                        if (!line.StartsWith("E-Mail: "))
                            return (object) line;
                        var addr = line.Substring("E-Mail: ".Length);
                        return new object[] { "E-Mail: ", new A { href = "mailto:" + addr }._(addr) };
                    })
                    .InsertBetween(new BR())
            );
        }
    }
}
