$(function () {
    function setHsl(h, s, l, skipRgb, noSnap) {
        h = ((h % 360 + 360) % 360) | 0;
        s = Math.min(100, Math.max(0, s)) | 0;
        l = Math.min(100, Math.max(0, l)) | 0;

        $('#colors_hsl').val(`hsl(${h}, ${s}%, ${l}%)`);
        $('#colors_hue').val(h);
        $('#colors_saturation').val(s);
        $('#colors_lightness').val(l);

        if (skipRgb)
            return setCursor(noSnap);
        var c = (1 - Math.abs(l / 50 - 1)) * (s / 100);
        var x = c * (1 - Math.abs((h / 60) % 2 - 1));
        var m = l / 100 - c / 2;
        var rgb$ =
            h < 60 ? [c, x, 0] :
                h < 120 ? [x, c, 0] :
                    h < 180 ? [0, c, x] :
                        h < 240 ? [0, x, c] :
                            h < 300 ? [x, 0, c] : [c, 0, x];
        setRgb(Math.round(255 * (rgb$[0] + m)), Math.round(255 * (rgb$[1] + m)), Math.round(255 * (rgb$[2] + m)), true, false, noSnap);
    }

    function rgbToHsl(r, g, b) {
        var r$ = r / 255, g$ = g / 255, b$ = b / 255, cx = Math.max(r$, g$, b$), cn = Math.min(r$, g$, b$), d = cx - cn;
        var h = d === 0 ? 0 :
            (r >= g && r >= b) ? Math.floor(60 * (((g$ - b$) / d) % 6)) :
                (g >= r && g >= b) ? Math.floor(60 * ((b$ - r$) / d + 2)) : Math.floor(60 * ((r$ - g$) / d + 4));
        if (h < 0) h += 360;
        var l = (cx + cn) / 2;
        return { h: h, s: d === 0 ? 0 : d * 100 / (1 - Math.abs(2 * l - 1)), l: l * 100 };
    }

    function setRgb(r, g, b, skipHsl, skipName, noSnap) {
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
            return setCursor(noSnap);
        var hsl = rgbToHsl(r, g, b);
        setHsl(hsl.h, hsl.s, hsl.l, true, noSnap);
    }

    $('#colors_red, #colors_green, #colors_blue').change(function () {
        setRgb(+$('#colors_red').val(), +$('#colors_green').val(), +$('#colors_blue').val());
    });

    $('#colors_rgb').change(function () {
        var v = $('#colors_rgb').val();
        var m;
        if (m = /^\s*rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)\s*$/.exec(v))
            setRgb(+m[1], +m[2], +m[3]);
    });

    $('#colors_hsl').change(function () {
        var v = $('#colors_hsl').val();
        var m;
        if (m = /^\s*hsl\s*\(\s*(\d+)\s*,\s*(\d+)\s*%\s*,\s*(\d+)\s*%\s*\)\s*$/.exec(v))
            setHsl(+m[1], +m[2], +m[3]);
    });

    function setHex(hex) {
        var m;
        if (m = /^\s*#?\s*([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])\s*$/.exec(hex))
            setRgb(parseInt(m[1] + m[1], 16), parseInt(m[2] + m[2], 16), parseInt(m[3] + m[3], 16));
        else if (m = /^\s*#?\s*([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})\s*$/.exec(hex))
            setRgb(parseInt(m[1], 16), parseInt(m[2], 16), parseInt(m[3], 16));
    }

    $('#colors_hex').change(function () { setHex($('#colors_hex').val()); });

    $('#colors_hue, #colors_saturation, #colors_lightness').change(function () {
        var h = +$('#colors_hue').val(), s = +$('#colors_saturation').val(), l = +$('#colors_lightness').val();
        setHsl(h, s, l);
    });

    function hexToRgb(hex) {
        return {
            r: hex.length === 4 ? 0x11 * parseInt(hex[1], 16) : parseInt(hex.substr(1, 2), 16),
            g: hex.length === 4 ? 0x11 * parseInt(hex[2], 16) : parseInt(hex.substr(3, 2), 16),
            b: hex.length === 4 ? 0x11 * parseInt(hex[3], 16) : parseInt(hex.substr(5, 2), 16)
        };
    }

    $('#colors_name').change(function () {
        var v = $('#colors_name').val();
        if (!v.length)
            return;
        var rgb = hexToRgb(v);
        setRgb(rgb.r, rgb.g, rgb.b, false, true);
    });

    var allColorValues = $('#colors_name > option:not(:first-child)').map(function (i, e) {
        var hex = $(e).val(), rgb = hexToRgb(hex), hsl = rgbToHsl(rgb.r, rgb.g, rgb.b);
        return { name: $(e).text(), hex: hex, r: rgb.r, g: rgb.g, b: rgb.b, h: hsl.h, s: hsl.s, l: hsl.l };
    }).get();

    function setXY(x, y) {
        $('svg>g:not(#pointer)').hide();
        $('g#' + x + '_' + y).show();
        setCursor();
    }

    function setCursor(noSnap) {
        $("#pointer").attr({
            fill: $('#colors_rgb').val(),
            stroke: $("#colors_lightness").val() > 50 ? "#000" : "#fff"
        });
        if (!noSnap)
            $("#pointer").attr({ transform: `translate(${getValueOnAxis($('#colors_x').val()) * 2552},${getValueOnAxis($('#colors_y').val()) * 2552})` });
    }

    (function makeDraggable(evt) {
        var svg = $("svg");

        svg.on('mousedown touchstart', startDrag);
        svg.on('mousemove touchmove', drag);
        svg.on('mouseup mouseleave touchend touchleave touchcancel', endDrag);

        svg = svg[0];

        var selectedElement, transform,
            minX, maxX, minY, maxY;

        var boundaryX1 = 0;
        var boundaryX2 = 2552;
        var boundaryY1 = 0;
        var boundaryY2 = 2552;

        function getMousePosition(evt) {
            var CTM = svg.getScreenCTM();
            if (evt.touches) { evt = evt.touches[0]; }
            return {
                x: (evt.clientX - CTM.e) / CTM.a,
                y: (evt.clientY - CTM.f) / CTM.d
            };
        }

        function startDrag(evt) {
            if (evt.target.parentNode.classList.contains('draggable')) {
                selectedElement = evt.target.parentNode;
                selectedElement.classList.add("grabbed");

                // Make sure the first transform on the element is a translate transform
                var transforms = selectedElement.transform.baseVal;

                if (transforms.length === 0 || transforms.getItem(0).type !== SVGTransform.SVG_TRANSFORM_TRANSLATE) {
                    // Create an transform that translates by (0, 0)
                    var translate = svg.createSVGTransform();
                    translate.setTranslate(0, 0);
                    selectedElement.transform.baseVal.insertItemBefore(translate, 0);
                }

                // Get initial translation
                transform = transforms.getItem(0);

                minX = boundaryX1;
                maxX = boundaryX2;
                minY = boundaryY1;
                maxY = boundaryY2;
            }
        }

        function drag(evt) {
            if (selectedElement) {
                evt.preventDefault();

                var coord = getMousePosition(evt);
                var dx = coord.x;
                var dy = coord.y;

                if (dx < minX) { dx = minX; }
                else if (dx > maxX) { dx = maxX; }
                if (dy < minY) { dy = minY; }
                else if (dy > maxY) { dy = maxY; }

                transform.setTranslate(dx, dy);

                updateDrag(dx, dy);
            }
        }

        function endDrag(evt) {
            if (selectedElement) {
                selectedElement.classList.remove("grabbed");
                var coord = getMousePosition(evt);
                var dx = coord.x;
                var dy = coord.y;

                if (dx < minX) { dx = minX; }
                else if (dx > maxX) { dx = maxX; }
                if (dy < minY) { dy = minY; }
                else if (dy > maxY) { dy = maxY; }

                updateDrag(dx, dy, true);
                selectedElement = false;
            }
        }
    })();

    function updateDrag(xv, yv, snap) {
        setValueOnAxis($('#colors_x').val(), xv / 2552, snap);
        setValueOnAxis($('#colors_y').val(), yv / 2552, snap);
    }

    function getValueOnAxis(axis) {
        switch (axis) {
            case 'r': return $("#colors_red").val() / 255;
            case 'g': return $("#colors_green").val() / 255;
            case 'b': return $("#colors_blue").val() / 255;
            case 'h': return $("#colors_hue").val() / 359;
            case 's': return $("#colors_saturation").val() / 100;
            case 'l': return $("#colors_lightness").val() / 100;
        }
    }

    function setValueOnAxis(axis, value, snap) {
        switch (axis) {
            case 'r': $("#colors_red").val((value * 255) | 0); setRgb($("#colors_red").val(), $("#colors_green").val(), $("#colors_blue").val(), false, false, !snap); break;
            case 'g': $("#colors_green").val((value * 255) | 0); setRgb($("#colors_red").val(), $("#colors_green").val(), $("#colors_blue").val(), false, false, !snap); break;
            case 'b': $("#colors_blue").val((value * 255) | 0); setRgb($("#colors_red").val(), $("#colors_green").val(), $("#colors_blue").val(), false, false, !snap); break;
            case 'h': $("#colors_hue").val((value * 359) | 0); setHsl($("#colors_hue").val(), $("#colors_saturation").val(), $("#colors_lightness").val(), false, !snap); break;
            case 's': $("#colors_saturation").val((value * 100) | 0); setHsl($("#colors_hue").val(), $("#colors_saturation").val(), $("#colors_lightness").val(), false, !snap); break;
            case 'l': $("#colors_lightness").val((value * 100) | 0); setHsl($("#colors_hue").val(), $("#colors_saturation").val(), $("#colors_lightness").val(), false, !snap); break;
        }
    }

    $('#colors_x, #colors_y').change(function () {
        var x = $('#colors_x').val(), y = $('#colors_y').val();
        if (x === y)
            $('#colors_y').val(y = (x === 'r' ? 'g' : 'r'));
        setXY(x, y);
    });
    setXY('r', 'g');

    $('.poly')
        .click(function () {
            setHex($(this).data('hex'));
            return false;
        })
        .mouseenter(function () {
            var hex = $(this).data('hex');
            $('#colors_name_tag').show().text($(this).data('name'));
            $('#colors_name_tag').css({
                left: $(this).position().left + this.getBoundingClientRect().width / 2,
                top: $(this).position().top + this.getBoundingClientRect().height / 2
            });
        })
        .mouseleave(function () {
            $('#colors_name_tag').hide();
        });
});