function expand_nav(item)
{
    // we could use this JQuery collapse function: item.parents(".panel-collapse").collapse("show");
    // but it is animated, and we don't want the animation in this case...

    // expand our parent panels so this item is visible in the TOC.
    item.addClass("active");
    item.parents(".panel-collapse").addClass("in");
    item.parents(".panel").find(".panel-heading").find("a").removeClass("collapsed").attr("aria-expanded", !0);
    item.parents(".panel").find(".panel-heading").addClass("active");

    // and if this item is also a panel then expand it downwards.
    var id = item.attr('capture')
    if (id) {
        id = "#collapse-" + id;
        $(id).addClass("in");
    }

    // make sure selected item is visible in the TOC by scrolling it into view.
    item[0].scrollIntoView();
}

function handle_resize()
{
    height = window.innerHeight;
    wrapper = $(".nav-left-wrapper")
    if (wrapper.length) {
        var top = $(".nav-left-wrapper").offset().top
        $(".nav-left-wrapper").height(height - top)
    } else {
        $(".navmenu").height(height);
    }
}

jQuery(document).ready(function ($) {
    $(window).resize(function(){
        handle_resize();
    });
    handle_resize();

    $(".navmenu").each(function() {
        permalink = $(this).attr("permalink");
        if (permalink.length){
            // ok we have one, so time to synchronize the expand/collapse state on the TOC.
            li = $("li").find("[permalink='" + permalink + "']");
            expand_nav(li);
        }
    });
});