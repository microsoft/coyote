// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

    // make sure selected item is visible in the TOC by scrolling it into view
    // unless the TOC is not scrollable (i.e. not "fixed") in which case we scroll
    // the main reference content into view instead
    var wrapper = $(".navmenu-fixed-left-wrapper");
    if (wrapper.length && wrapper.css("position") == "fixed")
    {
        item[0].scrollIntoView();
    }
    else
    {
        // on the phone this will jump us from selected TOC entry down to the
        // readable content on that topic.
        var content = $(".reference-content");
        if (content.length)
        {
            content[0].scrollIntoView();
        }
    }
}

function handle_resize()
{
    var height = window.outerHeight;
    var width = window.outerWidth;
    var wrapper = $(".nav-left-wrapper");
    if (!wrapper.length)
    {
        wrapper = $(".navbar-case-studies");
    }
    if (wrapper.length)
    {
        if (width >= 640) {
            var search_top = $(".search-wrapper").offset().top;
            var search_offset = search_top - window.scrollY;
            var top = wrapper.offset().top;
            var search_height = search_offset + top - search_top + 100;
            var scroller_height = height - search_height;
            wrapper.height(scroller_height);
        } else {
            wrapper.height("auto");
        }
    }
}

$(document).ready(function () {
    $(window).resize(function(){
        handle_resize();
    });
    handle_resize();

    $(".navmenu").each(function() {
        var permalink = $(this).attr("permalink");
        if (permalink.length){
            // ok we have one, so time to synchronize the expand/collapse state on the TOC.
            var li = $("li").find("[permalink='" + permalink + "']");
            if (li.length) {
                expand_nav(li);
            }
        }
    });
});