using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Tuulbox.Tools
{
    enum RotateMethod
    {
        Ninety,
        OneEighty,
        TwoSeventy
    }

    sealed class Rotate : ITuul
    {
        bool ITuul.Enabled { get { return false; } }
        string ITuul.Name { get { return "Rotate a picture"; } }
        string ITuul.Url { get { return "/rotate"; } }
        string ITuul.Keywords { get { return "rotate turn flip picture image photo"; } }
        string ITuul.Description { get { return "Rotates a picture you upload."; } }
        string ITuul.Js { get { return null; } }
        string ITuul.Css { get { return null; } }

        static RotateMethod[] _allowed = new[] { RotateMethod.Ninety, RotateMethod.OneEighty, RotateMethod.TwoSeventy };

        object ITuul.Handle(HttpRequest req)
        {
            if (req.Method == HttpMethod.Post)
            {
                // expect a file upload called "pic"
                if (!req.FileUploads.ContainsKey("pic"))
                    throw new HttpException(HttpStatusCode._400_BadRequest, userMessage: "No uploaded file specified.");
                var upload = req.FileUploads["pic"];

                var methodNullable = req.Post["rot"].Value.NullOr(m => EnumStrong.TryParse<RotateMethod>(m));
                if (methodNullable == null || !_allowed.Contains(methodNullable.Value))
                    throw new HttpException(HttpStatusCode._400_BadRequest, userMessage: "No rotation method specified.");
                var method = methodNullable.Value;

                using (var bitmap = new Bitmap(upload.GetStream()))
                using (var newBitmap = new Bitmap(
                    method == RotateMethod.OneEighty ? bitmap.Width : bitmap.Height,
                    method == RotateMethod.OneEighty ? bitmap.Height : bitmap.Width,
                    PixelFormat.Format32bppArgb))
                {
                    unsafe
                    {
                        var copyFromData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        var copyToData = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                        for (int y = 0; y < copyFromData.Height; y++)
                        {
                            byte* fromP = (byte*) copyFromData.Scan0 + y * copyFromData.Stride;
                            for (int x = 0; x < copyFromData.Width; x++)
                            {
                                var toX = method == RotateMethod.Ninety ? (bitmap.Height - 1) - y : method == RotateMethod.OneEighty ? (bitmap.Width - 1) - x : y;
                                var toY = method == RotateMethod.Ninety ? x : method == RotateMethod.OneEighty ? (bitmap.Height - 1) - y : (bitmap.Width - 1) - x;
                                byte* toP = (byte*) copyToData.Scan0 + toY * copyToData.Stride;
                                toP[4 * toX + 0] = fromP[4 * x + 0];
                                toP[4 * toX + 1] = fromP[4 * x + 1];
                                toP[4 * toX + 2] = fromP[4 * x + 2];
                                toP[4 * toX + 3] = fromP[4 * x + 3];
                            }
                        }
                        bitmap.UnlockBits(copyFromData);
                        newBitmap.UnlockBits(copyToData);
                    }

                    using (var memory = new MemoryStream())
                    {
                        newBitmap.Save(memory,
                            upload.ContentType == "image/bmp" ? ImageFormat.Png :
                            upload.ContentType == "image/gif" ? ImageFormat.Gif :
                            upload.ContentType == "image/jpeg" ? ImageFormat.Jpeg :
                            upload.ContentType == "image/png" ? ImageFormat.Png :
                            upload.ContentType == "image/tiff" ? ImageFormat.Tiff :
                            ImageFormat.Jpeg);
                        return HttpResponse.Create(memory.ToArray(), upload.ContentType);
                    }
                }
            }
            else
            {
                return new FORM { action = req.Url.ToHref(), enctype = enctype.multipart_formData, method = method.post }._(
                    new DIV("Choose file: ", new INPUT { type = itype.file, name = "pic" }),
                    new DIV("Rotate by: ", new SELECT { name = "rot" }._(
                        new OPTION { value = "Ninety" }._("90° to the right"),
                        new OPTION { value = "OneEighty" }._("180° (upside-down)"),
                        new OPTION { value = "TwoSeventy" }._("90° to the left")
                    )),
                    new DIV(new INPUT { type = itype.submit, value = "Rotate" }),
                    new DIV("After uploading, the rotated picture will be displayed in your browser. Press Ctrl+S then to save it to your disk.")
                );
            }
        }
    }
}
