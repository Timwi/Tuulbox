using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using RT.Servers;
using RT.TagSoup;

namespace Tuulbox.Tools
{
    sealed class ListAllTools : ITool
    {
        string ITool.Name { get { return "List of all tools"; } }
        string ITool.Url { get { return "/list"; } }
        string ITool.Keywords { get { return "complete list all tools"; } }
        string ITool.Description { get { return "Shows a list of all the tools available in the Toolboxx."; } }
        string ITool.Js { get { return null; } }
        string ITool.Css { get { return null; } }

        object ITool.Handle(HttpRequest req)
        {
            return new UL(
                Program.Tools.Where(tool => tool.Name != null).OrderBy(tool => tool.Name).Select(tool => new LI(
                    new DIV { class_ = "toolname" }._(new A { href = req.Url.WithPathParent().WithPathOnly(tool.Url).ToHref() }._(tool.Name)),
                    new DIV { class_ = "explain" }._(tool.Description)
                ))
            );
        }
    }
}
