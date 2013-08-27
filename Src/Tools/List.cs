using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using RT.Servers;
using RT.TagSoup;

namespace Tuulbox.Tools
{
    sealed class ListAllTools : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        string ITuul.Name { get { return "List of all tuuls"; } }
        string ITuul.Url { get { return "/list"; } }
        string ITuul.Keywords { get { return "complete list all tuuls"; } }
        string ITuul.Description { get { return "Shows a list of all the tuuls available in the Tuulbox."; } }
        string ITuul.Js { get { return null; } }
        string ITuul.Css { get { return null; } }

        object ITuul.Handle(HttpRequest req)
        {
            return new UL(
                Program.Tuuls.Where(tuul => tuul != this && tuul.Name != null).OrderBy(tuul => tuul.Name).Select(tuul => new LI(
                    new DIV { class_ = "tuulname" }._(new A { href = req.Url.WithPathParent().WithPathOnly(tuul.Url).ToHref() }._(tuul.Name)),
                    new DIV { class_ = "explain" }._(tuul.Description)
                ))
            );
        }
    }
}
