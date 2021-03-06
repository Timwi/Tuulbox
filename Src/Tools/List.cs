﻿using System;
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
        bool ITuul.Listed { get { return false; } }
        string ITuul.Name { get { return "List of all tuuls"; } }
        string ITuul.UrlName { get { return null; } }
        string ITuul.Keywords { get { return "complete list all tuuls"; } }
        string ITuul.Description { get { return "Shows a list of all the tuuls available in the Tuulbox."; } }
        string ITuul.Js { get { return null; } }
        string ITuul.Css { get { return null; } }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            return new UL(
                TuulboxModule.Tuuls.Where(tuul => tuul.Listed).OrderBy(tuul => tuul.Name).Select(tuul => new LI(
                    new DIV { class_ = "tuulname" }._(new A { href = req.Url.WithParents(2, tuul.UrlName, module.Settings.UseDomain).ToFull() }._(tuul.Name)),
                    new DIV { class_ = "explain" }._(tuul.Description)
                ))
            );
        }
    }
}
