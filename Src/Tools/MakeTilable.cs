using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using RT.Servers;
using RT.TagSoup;
using RT.Util.Drawing;

namespace Tuulbox.Tools;

internal sealed class MakeTilable : ITuul
{
    bool ITuul.Enabled => false;
    bool ITuul.Listed => false;
    string ITuul.Name => "Make tilable background";
    string ITuul.UrlName => "maketilable";
    string ITuul.Keywords => "make create tilable tile background backgrounds image picture repeat seamless";
    string ITuul.Description => "Takes an image you upload and generates a seamlessly tilable version of it.";
    string ITuul.Js => null;
    string ITuul.Css => null;

    object ITuul.Handle(TuulboxModule module, HttpRequest req)
    {
        if (req.Method == HttpMethod.Post)
        {
            // expect a file upload called "pic"
            if (!req.FileUploads.ContainsKey("pic"))
                throw new HttpException(HttpStatusCode._400_BadRequest, userMessage: "No uploaded file specified.");
            var upload = req.FileUploads["pic"];

            using (var bitmap = new Bitmap(upload.GetStream()))
            using (var newBitmap = MakeBitmapTilable(bitmap))
            using (var memory = new MemoryStream())
            {
                newBitmap.Save(memory,
                    upload.ContentType == "image/bmp" ? ImageFormat.Png :
                    upload.ContentType == "image/gif" ? ImageFormat.Png :
                    upload.ContentType == "image/jpeg" ? ImageFormat.Jpeg :
                    upload.ContentType == "image/png" ? ImageFormat.Png :
                    upload.ContentType == "image/tiff" ? ImageFormat.Tiff :
                    ImageFormat.Jpeg);
                return HttpResponse.Create(memory.ToArray(), upload.ContentType);
            }
        }

        return new FORM { action = req.Url.ToFull(), enctype = enctype.multipart_formData, method = method.post }._(
            new DIV("Choose file: ", new INPUT { type = itype.file, name = "pic" }),
            new DIV(new INPUT { type = itype.submit, value = "Make tilable" }),
            new DIV("After uploading, the tilable picture will be displayed in your browser. Press Ctrl+S then to save it to your disk.")
        );
    }

    private static Bitmap MakeBitmapTilable(Bitmap input)
    {
        var w = input.Width / 2;
        var h = input.Height / 2;

        var tilable = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var gr = Graphics.FromImage(tilable))
        {
            {
                gr.DrawImage(input, -w, 0);
                var horiz = GraphicsUtil.MakeSemitransparentImage(w, h, null,
                    g => { g.DrawImage(input, 0, 0); },
                    g => { g.FillRectangle(new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.Transparent, Color.White, 0f), new Rectangle(0, 0, w, h)); });
                gr.DrawImage(horiz, 0, 0);
            }
            var vert = GraphicsUtil.MakeSemitransparentImage(w, h, null,
                gv =>
                {
                    gv.DrawImage(input, -w, -h);
                    var horiz = GraphicsUtil.MakeSemitransparentImage(w, h, null,
                        g => { g.DrawImage(input, 0, -h); },
                        g => { g.FillRectangle(new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.Transparent, Color.White, 0f), new Rectangle(0, 0, w, h)); });
                    gv.DrawImage(horiz, 0, 0);
                },
                gv => { gv.FillRectangle(new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.White, Color.Transparent, 90f), new Rectangle(0, 0, w, h)); });
            gr.DrawImage(vert, 0, 0);
        }
        return tilable;
    }
}
