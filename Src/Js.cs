using RT.Json;
using RT.Servers;
using RT.Util.ExtensionMethods;

namespace Tuulbox
{
    sealed class Js : ITuul
    {
        private static byte[] _js = JsonValue.Fmt(@"

$(function()
{
    $('.tab-control').each(function()
    {
        var tabcontrol = $(this);
        var tabs = tabcontrol.find('div.tabs > a');
        var switchTab = function(lnk)
        {
            tabs.removeClass('selected');
            lnk.addClass('selected');
            tabcontrol.find('div.tab').hide();
            tabcontrol.find('div.tab#' + lnk.data('tab')).show();
        };
        switchTab(tabcontrol.find('div.tabs > a.selected'));
        tabs.click(function()
        {
            switchTab($(this));
            return false;
        });
    });
});

").ToUtf8();

        object ITuul.Handle(TuulboxModule module, HttpRequest req)
        {
            return HttpResponse.JavaScript(_js);
        }

        bool ITuul.Enabled { get { return true; } }
        bool ITuul.Listed { get { return false; } }
        string ITuul.Name { get { return null; } }
        string ITuul.UrlName { get { return "js"; } }
        string ITuul.Keywords { get { return null; } }
        string ITuul.Description { get { return null; } }
        string ITuul.Js { get { return null; } }
        string ITuul.Css { get { return null; } }
    }
}
