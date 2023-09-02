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