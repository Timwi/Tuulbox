using RT.Servers;
using RT.TagSoup;
using RT.Util;

namespace Tuulbox
{
    partial class TuulboxModule
    {
        HttpResponse handle(HttpRequest req, ITuul tuul)
        {
            var content = tuul.Handle(this, req);
            if (content is HttpResponse)
                return (HttpResponse) content;

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE(tuul.Name),
                    new LINK { rel = "stylesheet", type = "text/css", href = req.Url.WithParents(2, "css", Settings.UseDomain).ToFull() },
                    tuul.Css.NullOr(css => new STYLELiteral(css)),
                    new SCRIPT { src = "//ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js" },
                    new SCRIPT { src = "//ajax.googleapis.com/ajax/libs/jqueryui/1.9.0/jquery-ui.min.js" },
                    new SCRIPT { src = req.Url.WithParents(2, "js", Settings.UseDomain).ToFull() },
                    tuul.Js.NullOr(js => new SCRIPTLiteral(js))
                ),
                new BODY(
                    new DIV { class_ = "everything" }._(
                        new DIV { class_ = "search" }._(
                            //new H2("Find a tuul:"),
                            //new DIV(new INPUT { type = itype.text }),
                            new DIV(new A { href = req.Url.WithParents(2, "", Settings.UseDomain).ToFull() }._("List of all tuuls"))
                        ),
                        new H1("Tuulbox"),
                        new DIV { class_ = "content" }._(
                            new H2(tuul.Name),
                            content
                        )
                    ),
                    new DIV { class_ = "footer" }._(
                        new A { href = "https://legal.timwi.de" }._("Legal · Impressum · Datenschutzerklärung")
                    )
                )
            ));
        }
    }
}
