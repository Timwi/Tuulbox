﻿using System.Collections.Generic;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util.ExtensionMethods;

namespace Tuulbox.Tools
{
    sealed class Colors : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return true; } }
        string ITuul.Name { get { return "Colors"; } }
        string ITuul.UrlName { get { return "colors"; } }
        string ITuul.Keywords { get { return "color colour colors colours css name names rgb to hsl hex hexadecimal convert conversion"; } }
        string ITuul.Description { get { return "Converts CSS color values between RGB, HSL, and color names."; } }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            return new DIV(
                new TABLE(
                    new COL(), new COL(), new COL(), new COL(), new COL { class_ = "colors_big" },
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Red", "e", "colors_red")), new TD(new INPUT { type = itype.number, min = "0", max = "255", id = "colors_red", name = "red", value = "0" }),
                        new TD(Helpers.LabelWithAccessKey("Hue", "u", "colors_hue")), new TD(new INPUT { type = itype.number, min = "0", max = "359", id = "colors_hue", name = "hue", value = "0" }),
                        new TD { rowspan = 5, id = "colors_color" }),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Green", "g", "colors_green")), new TD(new INPUT { type = itype.number, min = "0", max = "255", id = "colors_green", name = "green", value = "0" }),
                        new TD(Helpers.LabelWithAccessKey("Saturation", "s", "colors_saturation")), new TD(new INPUT { type = itype.number, step = "0.001", min = "0", max = "1", id = "colors_saturation", name = "saturation", value = "0" })),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Blue", "b", "colors_blue")), new TD(new INPUT { type = itype.number, min = "0", max = "255", id = "colors_blue", name = "blue", value = "0" }),
                        new TD(Helpers.LabelWithAccessKey("Lightness", "l", "colors_lightness")), new TD(new INPUT { type = itype.number, step = "0.001", min = "0", max = "1", id = "colors_lightness", name = "lightness", value = "0" })),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("RGB", "r", "colors_rgb")), new TD(new INPUT { type = itype.text, id = "colors_rgb", name = "rgb", value = "rgb(0, 0, 0)" }),
                        new TD(Helpers.LabelWithAccessKey("HSL", "h", "colors_hsl")), new TD(new INPUT { type = itype.text, id = "colors_hsl", name = "hsl", value = "hsl(0, 0, 0)" })),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Hex", "x", "colors_hex")), new TD(new INPUT { type = itype.text, id = "colors_hex", name = "hex", value = "#000" }),
                        new TD(Helpers.LabelWithAccessKey("Name", "n", "colors_name")),
                        new TD(
                            new SELECT { id = "colors_name", name = "name" }._(
                                new OPTION { value = "" }._("(none)").Concat(_names.Select(kvp => new OPTION { value = kvp.Value, selected = kvp.Key == "Black" }._(kvp.Key))))))));
        }

        string ITuul.Js
        {
            get
            {
                return @"

$(function()
{
    function setHsl(h, s, l, skipRgb)
    {
        if (h < 0 || s < 0 || l < 0 || h > 359 || s > 1 || l > 1)
            return;
        h = +h.toFixed(3);
        s = +s.toFixed(3);
        l = +l.toFixed(3);
        $('#colors_hsl').val('hsl(' + h + ', ' + s + ', ' + l + ')');
        $('#colors_hue').val(h);
        $('#colors_saturation').val(s);
        $('#colors_lightness').val(l);
    }

    function setRgb(r, g, b, skipHsl, skipName)
    {
        if (r < 0 || g < 0 || b < 0 || r > 255 || g > 255 || b > 255)
            return;
        $('#colors_rgb').val('rgb(' + r + ', ' + g + ', ' + b + ')');
        var rhex = ('0' + r.toString(16)).substr(-2), ghex = ('0' + g.toString(16)).substr(-2), bhex = ('0' + b.toString(16)).substr(-2);
        var hex = '#' + (rhex[0] === rhex[1] && ghex[0] === ghex[1] && bhex[0] === bhex[1] ? rhex[0] + ghex[0] + bhex[0] : rhex + ghex + bhex);
        $('#colors_hex').val(hex);
        $('#colors_red').val(r);
        $('#colors_green').val(g);
        $('#colors_blue').val(b);
        $('#colors_color').css('background-color', hex);

        if (!skipName)
            $('#colors_name').val(hex);

        if (skipHsl)
            return;
        var r$ = r/255, g$ = g/255, b$ = b/255, cx = Math.max(r$, g$, b$), cn = Math.min(r$, g$, b$), d = cx-cn;
        var h = d === 0 ? 0 :
            (r > g && r > b) ? Math.floor(60 * (((g$ - b$)/d) % 6)) :
            (g > r && g > b) ? Math.floor(60 * ((b$ - r$)/d + 2)) : Math.floor(60 * ((r$ - g$)/d + 4));
        if (h < 0) h += 360;
        var l = (cx + cn)/2;
        var s = d === 0 ? 0 : d/(1 - Math.abs(2*l - 1));
        setHsl(h, s, l, true);
    }

    $('#colors_red, #colors_green, #colors_blue').change(function()
    {
        setRgb(+$('#colors_red').val(), +$('#colors_green').val(), +$('#colors_blue').val());
    });

    $('#colors_rgb').change(function()
    {
        var v = $('#colors_rgb').val();
        if (/^\s*rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)\s*$/.test(v))
            setRgb(+RegExp.$1, +RegExp.$2, +RegExp.$3);
    });

    $('#colors_hex').change(function()
    {
        var v = $('#colors_hex').val();
        if (/^\s*#?\s*([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])\s*$/.test(v))
            setRgb(parseInt(RegExp.$1 + RegExp.$1, 16), parseInt(RegExp.$2 + RegExp.$2, 16), parseInt(RegExp.$3 + RegExp.$3, 16));
        else if (/^\s*#?\s*([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})\s*$/.test(v))
            setRgb(parseInt(RegExp.$1, 16), parseInt(RegExp.$2, 16), parseInt(RegExp.$3, 16));
    });

    $('#colors_hue, #colors_saturation, #colors_lightness').change(function()
    {
        var h = +$('#colors_hue').val(), s = +$('#colors_saturation').val(), l = +$('#colors_lightness').val();
        setHsl(h, s, l);
    });

    $('#colors_name').change(function()
    {
        var v = $('#colors_name').val();
        if (!v.length)
            return;
        setRgb(
            v.length === 4 ? 0x11 * parseInt(v[1], 16) : parseInt(v.substr(1, 2), 16),
            v.length === 4 ? 0x11 * parseInt(v[2], 16) : parseInt(v.substr(3, 2), 16),
            v.length === 4 ? 0x11 * parseInt(v[3], 16) : parseInt(v.substr(5, 2), 16),
            false, true);
    });
});

                ";
            }
        }

        string ITuul.Css
        {
            get
            {
                return @"
                    table { width: 100%; border-collapse: collapse; empty-cells: show; }
                    col.colors_big { width: 100%; }
                    td:not(#colors_color) { padding-right: .5em; }
                    #colors_color { background-color: black; }
                ";
            }
        }

        public static Dictionary<string, string> _names = new Dictionary<string, string>
        {
            { "AliceBlue", "#f0f8ff" },
            { "AntiqueWhite", "#faebd7" },
            { "Aqua", "#0ff" },
            { "Aquamarine", "#7fffd4" },
            { "Azure", "#f0ffff" },
            { "Beige", "#f5f5dc" },
            { "Bisque", "#ffe4c4" },
            { "Black", "#000" },
            { "BlanchedAlmond", "#ffebcd" },
            { "Blue", "#00f" },
            { "BlueViolet", "#8a2be2" },
            { "Brown", "#a52a2a" },
            { "BurlyWood", "#deb887" },
            { "CadetBlue", "#5f9ea0" },
            { "Chartreuse", "#7fff00" },
            { "Chocolate", "#d2691e" },
            { "Coral", "#ff7f50" },
            { "CornflowerBlue", "#6495ed" },
            { "Cornsilk", "#fff8dc" },
            { "Crimson", "#dc143c" },
            { "Cyan", "#0ff" },
            { "DarkBlue", "#00008b" },
            { "DarkCyan", "#008b8b" },
            { "DarkGoldenrod", "#b8860b" },
            { "DarkGray", "#a9a9a9" },
            { "DarkGreen", "#006400" },
            { "DarkKhaki", "#bdb76b" },
            { "DarkMagenta", "#8b008b" },
            { "DarkOliveGreen", "#556b2f" },
            { "DarkOrange", "#ff8c00" },
            { "DarkOrchid", "#9932cc" },
            { "DarkRed", "#8b0000" },
            { "DarkSalmon", "#e9967a" },
            { "DarkSeaGreen", "#8fbc8f" },
            { "DarkSlateBlue", "#483d8b" },
            { "DarkSlateGray", "#2f4f4f" },
            { "DarkTurquoise", "#00ced1" },
            { "DarkViolet", "#9400d3" },
            { "DeepPink", "#ff1493" },
            { "DeepSkyBlue", "#00bfff" },
            { "DimGray", "#696969" },
            { "DodgerBlue", "#1e90ff" },
            { "FireBrick", "#b22222" },
            { "FloralWhite", "#fffaf0" },
            { "ForestGreen", "#228b22" },
            { "Fuchsia", "#f0f" },
            { "Gainsboro", "#dcdcdc" },
            { "GhostWhite", "#f8f8ff" },
            { "Gold", "#ffd700" },
            { "Goldenrod", "#daa520" },
            { "Gray", "#808080" },
            { "Green", "#008000" },
            { "GreenYellow", "#adff2f" },
            { "Honeydew", "#f0fff0" },
            { "HotPink", "#ff69b4" },
            { "IndianRed", "#cd5c5c" },
            { "Indigo", "#4b0082" },
            { "Ivory", "#fffff0" },
            { "Khaki", "#f0e68c" },
            { "Lavender", "#e6e6fa" },
            { "LavenderBlush", "#fff0f5" },
            { "LawnGreen", "#7cfc00" },
            { "LemonChiffon", "#fffacd" },
            { "LightBlue", "#add8e6" },
            { "LightCoral", "#f08080" },
            { "LightCyan", "#e0ffff" },
            { "LightGoldenrodYellow", "#fafad2" },
            { "LightGreen", "#90ee90" },
            { "LightGrey", "#d3d3d3" },
            { "LightPink", "#ffb6c1" },
            { "LightSalmon", "#ffa07a" },
            { "LightSeaGreen", "#20b2aa" },
            { "LightSkyBlue", "#87cefa" },
            { "LightSlateGray", "#789" },
            { "LightSteelBlue", "#b0c4de" },
            { "LightYellow", "#ffffe0" },
            { "Lime", "#0f0" },
            { "LimeGreen", "#32cd32" },
            { "Linen", "#faf0e6" },
            { "Magenta", "#f0f" },
            { "Maroon", "#800000" },
            { "MediumAquamarine", "#66cdaa" },
            { "MediumBlue", "#0000cd" },
            { "MediumOrchid", "#ba55d3" },
            { "MediumPurple", "#9370db" },
            { "MediumSeaGreen", "#3cb371" },
            { "MediumSlateBlue", "#7b68ee" },
            { "MediumSpringGreen", "#00fa9a" },
            { "MediumTurquoise", "#48d1cc" },
            { "MediumVioletRed", "#c71585" },
            { "MidnightBlue", "#191970" },
            { "MintCream", "#f5fffa" },
            { "MistyRose", "#ffe4e1" },
            { "Moccasin", "#ffe4b5" },
            { "NavajoWhite", "#ffdead" },
            { "Navy", "#000080" },
            { "OldLace", "#fdf5e6" },
            { "Olive", "#808000" },
            { "OliveDrab", "#6b8e23" },
            { "Orange", "#ffa500" },
            { "OrangeRed", "#ff4500" },
            { "Orchid", "#da70d6" },
            { "PaleGoldenrod", "#eee8aa" },
            { "PaleGreen", "#98fb98" },
            { "PaleTurquoise", "#afeeee" },
            { "PaleVioletRed", "#db7093" },
            { "PapayaWhip", "#ffefd5" },
            { "PeachPuff", "#ffdab9" },
            { "Peru", "#cd853f" },
            { "Pink", "#ffc0cb" },
            { "Plum", "#dda0dd" },
            { "PowderBlue", "#b0e0e6" },
            { "Purple", "#800080" },
            { "Red", "#f00" },
            { "RosyBrown", "#bc8f8f" },
            { "RoyalBlue", "#4169e1" },
            { "SaddleBrown", "#8b4513" },
            { "Salmon", "#fa8072" },
            { "SandyBrown", "#f4a460" },
            { "SeaGreen", "#2e8b57" },
            { "Seashell", "#fff5ee" },
            { "Sienna", "#a0522d" },
            { "Silver", "#c0c0c0" },
            { "SkyBlue", "#87ceeb" },
            { "SlateBlue", "#6a5acd" },
            { "SlateGray", "#708090" },
            { "Snow", "#fffafa" },
            { "SpringGreen", "#00ff7f" },
            { "SteelBlue", "#4682b4" },
            { "Tan", "#d2b48c" },
            { "Teal", "#008080" },
            { "Thistle", "#d8bfd8" },
            { "Tomato", "#ff6347" },
            { "Turquoise", "#40e0d0" },
            { "Violet", "#ee82ee" },
            { "Wheat", "#f5deb3" },
            { "White", "#fff" },
            { "WhiteSmoke", "#f5f5f5" },
            { "Yellow", "#ff0" },
            { "YellowGreen", "#9acd32" },
        };
    }
}
