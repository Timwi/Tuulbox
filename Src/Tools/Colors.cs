using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using RT.KitchenSink.Geometry;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Geometry;

namespace Tuulbox.Tools
{
    sealed class Colors : ITuul
    {
        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return true; } }
        string ITuul.Name { get { return "Convert RGB/HSL/color names"; } }
        string ITuul.UrlName { get { return "colors"; } }
        string ITuul.Keywords { get { return "color colour colors colours css name names rgb to hsl hex hexadecimal convert conversion"; } }
        string ITuul.Description { get { return "Converts CSS color values between RGB, HSL, and color names."; } }

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            return new DIV(
                new TABLE(
                    new COL(), new COL(), new COL(), new COL(), new COL { class_ = "colors_big" },
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Red:", "d", "colors_red")), new TD(new INPUT { type = itype.number, min = "0", max = "255", id = "colors_red", name = "red", value = "0" }),
                        new TD(Helpers.LabelWithAccessKey("Hue:", "u", "colors_hue")), new TD(new INPUT { type = itype.number, min = "0", max = "359", id = "colors_hue", name = "hue", value = "0" }),
                        new TD { rowspan = 5, id = "colors_color" }),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Green:", "g", "colors_green")), new TD(new INPUT { type = itype.number, min = "0", max = "255", id = "colors_green", name = "green", value = "0" }),
                        new TD(Helpers.LabelWithAccessKey("Saturation:", "s", "colors_saturation")), new TD(new INPUT { type = itype.number, step = "1", min = "0", max = "100", id = "colors_saturation", name = "saturation", value = "0" })),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Blue:", "b", "colors_blue")), new TD(new INPUT { type = itype.number, min = "0", max = "255", id = "colors_blue", name = "blue", value = "0" }),
                        new TD(Helpers.LabelWithAccessKey("Lightness:", "l", "colors_lightness")), new TD(new INPUT { type = itype.number, step = "1", min = "0", max = "100", id = "colors_lightness", name = "lightness", value = "0" })),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("RGB:", "r", "colors_rgb")), new TD(new INPUT { type = itype.text, id = "colors_rgb", name = "rgb", value = "rgb(0, 0, 0)" }),
                        new TD(Helpers.LabelWithAccessKey("HSL:", "h", "colors_hsl")), new TD(new INPUT { type = itype.text, id = "colors_hsl", name = "hsl", value = "hsl(0, 0%, 0%)" })),
                    new TR(
                        new TD(Helpers.LabelWithAccessKey("Hex:", "e", "colors_hex")), new TD(new INPUT { type = itype.text, id = "colors_hex", name = "hex", value = "#000" }),
                        new TD(Helpers.LabelWithAccessKey("Name:", "n", "colors_name")),
                        new TD(
                            new SELECT { id = "colors_name", name = "name" }._(
                                new OPTION { value = "" }._("(none)").Concat(_names.Select(kvp => new OPTION { value = kvp.Value, selected = kvp.Key == "Black" }._(kvp.Key))))))),
                    new HR(),
                    new DIV { id = "colors_xycontrols" }._(
                        Helpers.LabelWithAccessKey("X:", "x", "colors_x"), " ", new SELECT { id = "colors_x" }._(_sortableCriteria.Select(kvp => new OPTION { value = kvp.Key, selected = kvp.Key == "r" }._(kvp.Value))), " ",
                        Helpers.LabelWithAccessKey("Y:", "y", "colors_y"), " ", new SELECT { id = "colors_y" }._(_sortableCriteria.Select(kvp => new OPTION { value = kvp.Key, selected = kvp.Key == "g" }._(kvp.Value)))),
                    _svgs,
                    new DIV { id = "colors_name_tag" });
        }

        static Dictionary<string, string> _sortableCriteria = new Dictionary<string, string>
        {
            { "r", "Red" },
            { "g", "Green" },
            { "b", "Blue" },
            { "h", "Hue" },
            { "s", "Saturation" },
            { "l", "Lightness" }
        };

        string ITuul.Js
        {
            get
            {
                return @"

$(function()
{
    function setHsl(h, s, l, skipRgb)
    {
        h = Math.min(359, Math.max(0, h)) | 0;
        s = Math.min(100, Math.max(0, s)) | 0;
        l = Math.min(100, Math.max(0, l)) | 0;

        $('#colors_hsl').val(`hsl(${h}, ${s}%, ${l}%)`);
        $('#colors_hue').val(h);
        $('#colors_saturation').val(s);
        $('#colors_lightness').val(l);

        if (skipRgb)
            return;
        var c = (1 - Math.abs(l/50 - 1)) * (s/100);
        var x = c * (1 - Math.abs((h/60) % 2 - 1));
        var m = l/100 - c/2;
        var rgb$ =
            h < 60 ? [ c, x, 0 ] :
            h < 120 ? [ x, c, 0 ] :
            h < 180 ? [ 0, c, x ] :
            h < 240 ? [ 0, x, c ] :
            h < 300 ? [ x, 0, c ] : [ c, 0, x ];
        setRgb(Math.round(255 * (rgb$[0] + m)), Math.round(255 * (rgb$[1] + m)), Math.round(255 * (rgb$[2] + m)), true);
    }

    function rgbToHsl(r, g, b)
    {
        var r$ = r/255, g$ = g/255, b$ = b/255, cx = Math.max(r$, g$, b$), cn = Math.min(r$, g$, b$), d = cx-cn;
        var h = d === 0 ? 0 :
            (r >= g && r >= b) ? Math.floor(60 * (((g$ - b$)/d) % 6)) :
            (g >= r && g >= b) ? Math.floor(60 * ((b$ - r$)/d + 2)) : Math.floor(60 * ((r$ - g$)/d + 4));
        if (h < 0) h += 360;
        var l = (cx + cn)/2;
        return { h: h, s: d === 0 ? 0 : d*100/(1 - Math.abs(2*l - 1)), l: l*100 };
    }

    function setRgb(r, g, b, skipHsl, skipName)
    {
        r = Math.min(255, Math.max(0, r)) | 0;
        g = Math.min(255, Math.max(0, g)) | 0;
        b = Math.min(255, Math.max(0, b)) | 0;

        $('#colors_rgb').val(`rgb(${r}, ${g}, ${b})`);
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
        var hsl = rgbToHsl(r, g, b);
        setHsl(hsl.h, hsl.s, hsl.l, true);
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

    function setHex(hex)
    {
        if (/^\s*#?\s*([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])\s*$/.test(hex))
            setRgb(parseInt(RegExp.$1 + RegExp.$1, 16), parseInt(RegExp.$2 + RegExp.$2, 16), parseInt(RegExp.$3 + RegExp.$3, 16));
        else if (/^\s*#?\s*([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})\s*$/.test(hex))
            setRgb(parseInt(RegExp.$1, 16), parseInt(RegExp.$2, 16), parseInt(RegExp.$3, 16));
    }

    $('#colors_hex').change(function() { setHex($('#colors_hex').val()); });

    $('#colors_hue, #colors_saturation, #colors_lightness').change(function()
    {
        var h = +$('#colors_hue').val(), s = +$('#colors_saturation').val(), l = +$('#colors_lightness').val();
        setHsl(h, s, l);
    });

    function hexToRgb(hex)
    {
        return {
            r: hex.length === 4 ? 0x11 * parseInt(hex[1], 16) : parseInt(hex.substr(1, 2), 16),
            g: hex.length === 4 ? 0x11 * parseInt(hex[2], 16) : parseInt(hex.substr(3, 2), 16),
            b: hex.length === 4 ? 0x11 * parseInt(hex[3], 16) : parseInt(hex.substr(5, 2), 16)
        };
    }

    $('#colors_name').change(function()
    {
        var v = $('#colors_name').val();
        if (!v.length)
            return;
        var rgb = hexToRgb(v);
        setRgb(rgb.r, rgb.g, rgb.b, false, true);
    });

    var allColorValues = $('#colors_name > option:not(:first-child)').map(function(i, e) {
        var hex = $(e).val(), rgb = hexToRgb(hex), hsl = rgbToHsl(rgb.r, rgb.g, rgb.b);
        return { name: $(e).text(), hex: hex, r: rgb.r, g: rgb.g, b: rgb.b, h: hsl.h, s: hsl.s, l: hsl.l };
    }).get();

    function setXY(x, y)
    {
        $('svg').hide();
        $('#' + x + '_' + y).show();
    }

    $('#colors_x, #colors_y').change(function()
    {
        var x = $('#colors_x').val(), y = $('#colors_y').val();
        if (x === y)
            $('#colors_y').val(y = (x === 'r' ? 'g' : 'r'));
        setXY(x, y);
    });
    setXY('r', 'g');

    $('.poly')
        .click(function()
        {
            setHex($(this).data('hex'));
            return false;
        })
        .mouseenter(function()
        {
            var hex = $(this).data('hex');
            $('#colors_name_tag').show().text($(this).data('name'));
            $('#colors_name_tag').css({
                left: $(this).position().left + this.getBoundingClientRect().width/2,
                top: $(this).position().top + this.getBoundingClientRect().height/2
            });
        })
        .mouseleave(function()
        {
            $('#colors_name_tag').hide();
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
                    #colors_xycontrols { margin-bottom: .2em; }
                    #colors_name_tag {
                        display: none;
                        position: absolute;
                        border: 2px solid black;
                        border-radius: .5em;
                        box-shadow: 5px 5px 5px rgba(0, 0, 0, .3);
                        background: white;
                        padding: .5em 1em;
                        pointer-events: none;
                        transform: translate(-50%, -50%);
                    }
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

        private static RawTag _svgsCache;
        private static RawTag _svgs { get { return _svgsCache ?? (_svgsCache = generateSvgs()); } }

        static RawTag generateSvgs()
        {
            var dic = new Dictionary<string, string>();
            var rnd = new Random(1);

            var colors = _names.Select(kvp =>
            {
                var r = int.Parse(kvp.Value.Length == 4 ? "" + kvp.Value[1] + kvp.Value[1] : kvp.Value.Substring(1, 2), NumberStyles.HexNumber);
                var g = int.Parse(kvp.Value.Length == 4 ? "" + kvp.Value[2] + kvp.Value[2] : kvp.Value.Substring(3, 2), NumberStyles.HexNumber);
                var b = int.Parse(kvp.Value.Length == 4 ? "" + kvp.Value[3] + kvp.Value[3] : kvp.Value.Substring(5, 2), NumberStyles.HexNumber);

                double r_ = r / 255d, g_ = g / 255d, b_ = b / 255d, cx = Ut.Max(r_, g_, b_), cn = Ut.Min(r_, g_, b_), d = cx - cn;
                var h = d == 0 ? 0 :
                    (r >= g && r >= b) ? Math.Floor(60 * (((g_ - b_) / d) % 6)) :
                    (g >= r && g >= b) ? Math.Floor(60 * ((b_ - r_) / d + 2)) : Math.Floor(60 * ((r_ - g_) / d + 4));
                if (h < 0)
                    h += 360;
                var l = (cx + cn) / 2;
                var s = d == 0 ? 0 : d / (1 - Math.Abs(2 * l - 1));

                return new
                {
                    Name = kvp.Key,
                    Dic = new Dictionary<string, double> { { "r", 10 * r }, { "g", 10 * g }, { "b", 10 * b }, { "h", h * 2550 / 360 }, { "s", s * 2550 }, { "l", l * 2550 } },
                    R = r,
                    G = g,
                    B = b,
                    Hex = kvp.Value
                };
            }).ToArray();

            var svg = new StringBuilder();

            foreach (var x in _sortableCriteria.Keys)
                foreach (var y in _sortableCriteria.Keys)
                {
                    if (x == y)
                        continue;

                    var sites = colors.Select(inf => new PointD(inf.Dic[x] + .5 + rnd.NextDouble(), inf.Dic[y] + .5 + rnd.NextDouble())).ToArray();
                    var diagram = VoronoiDiagram.GenerateVoronoiDiagram(sites, 2552, 2552, VoronoiDiagramFlags.IncludeEdgePolygons);

                    svg.Append("<svg xmlns='http://www.w3.org/2000/svg' viewbox='0 0 2552 2552' width='100%' id='{0}_{1}'>".Fmt(x, y));
                    for (var polyIx = 0; polyIx < diagram.Polygons.Length; polyIx++)
                    {
                        var kvp = diagram.Polygons[polyIx];
                        svg.Append("<path class='poly' data-hex='{4}' data-name='{5}' style='fill:#{0:X2}{1:X2}{2:X2}' d='M {3} z'></path>".Fmt(
                            /* {0}–{2} */ colors[polyIx].R, colors[polyIx].G, colors[polyIx].B,
                            /* {3} */ kvp.Vertices.Select(p => Math.Round(p.X) + " " + Math.Round(p.Y)).JoinString(" "),
                            /* {4} */ colors[polyIx].Hex,
                            /* {5} */ colors[polyIx].Name));
                    }

                    svg.Append("</svg>");
                }

            return new RawTag(svg.ToString());
        }
    }
}
