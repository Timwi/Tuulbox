using RT.Servers;
using RT.TagSoup;
using RT.Util;

namespace Tuulbox
{
    partial class Program
    {
        static HttpResponse handle(HttpRequest req, ITuul tuul)
        {
            var content = tuul.Handle(req);
            if (content is HttpResponse)
                return (HttpResponse) content;

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE(tuul.Name),
                    new LINK { rel = "stylesheet", type = "text/css", href = req.Url.WithPathParent().WithPathOnly("/css").ToHref() },
                    tuul.Css.NullOr(css => new LINK { rel = "stylesheet", type = "text/css", href = req.Url.WithPathOnly("/css").ToHref() }),
                    new SCRIPT { src = "http://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js" },
                    new SCRIPT { src = "http://ajax.googleapis.com/ajax/libs/jqueryui/1.9.0/jquery-ui.min.js" },
                    new SCRIPT { src = req.Url.WithPathParent().WithPathOnly("/js").ToHref() },
                    tuul.Js.NullOr(js => new SCRIPT { src = req.Url.WithPathOnly("/js").ToHref() })
                ),
                new BODY(
                    new DIV { class_ = "everything" }._(
                        new DIV { class_ = "search" }._(
                            //new H2("Find a tuul:"),
                            //new DIV(new INPUT { type = itype.text }),
                            new DIV(new A { href = req.Url.WithPathParent().WithPathOnly("/list").ToHref() }._("List of all tuuls"))
                        ),
                        new H1("Tuulbox"),
                        new DIV { class_ = "content" }._(
                            new H2(tuul.Name),
                            content
                        )
                    )
                )
            ));
        }
    }
}
