using System;
using RT.Servers;
using RT.TagSoup;
using RT.Util.ExtensionMethods;
using RT.Util;

namespace Tuulbox.Tools;

internal sealed class Base64 : ITuul
{
    bool ITuul.Enabled => true;
    bool ITuul.Listed => true;
    string ITuul.Name => "Base-64 encoder/decoder";
    string ITuul.UrlName => "base64";
    string ITuul.Keywords => "base 64 base64 base-64 encode decode encoder decoder transcoder converter";
    string ITuul.Description => "Converts text you provide to and from base-64.";
    string ITuul.Js => null;
    string ITuul.Css => @"
                    col { width: 50%; }
                    col.spacer { width: 1em; }
                ";

    object ITuul.Handle(TuulboxModule module, HttpRequest req)
    {
        string input = null;
        object encoded = null;
        object decoded = null;

        if (req.Method == HttpMethod.Post)
        {
            input = req.Post["input"].Value;
            if (input != null)
            {
                var encodedStr = Convert.ToBase64String(input.ToUtf8(), Base64FormattingOptions.InsertLineBreaks);
                encoded = new object[] { new DIV("Base-64 encoding of the provided text:"), new DIV(new TEXTAREA { readonly_ = true }._(encodedStr)) };
                var decodedStr = Ut.OnExceptionDefault(() => Convert.FromBase64String(input).FromUtf8(), null);
                if (decodedStr != null)
                    decoded = new object[] { new DIV("Decoded text:"), new DIV(new TEXTAREA { readonly_ = true }._(decodedStr)) };
            }
        }

        return Ut.NewArray(
            new FORM { action = req.Url.ToFull(), method = method.post }._(
                new H3("Text to encode/decode"),
                new DIV(new TEXTAREA { name = "input", accesskey = "," }._(input)),
                new DIV(new BUTTON { type = btype.submit, accesskey = "g" }._(Helpers.TextWithAccessKey("Go for it", "g")))
            ),

            encoded != null || decoded != null ? new H3("Result") : null,
            encoded != null && decoded != null ? new TABLE { class_ = "layout" }._(new COL(), new COL { class_ = "spacer" }, new COL(), new TR(new TD(encoded), new TD(), new TD(decoded))) :
            (object) new[] { encoded, decoded }
        );
    }
}
