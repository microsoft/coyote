jQuery(document).ready(function($) {
    
    // shrink nav onscroll - mobile first ux
    $(window).scroll(function() {
        if ($(document).scrollTop() > 20) {
            $('.navbar-default').addClass('shrink');
        } else {
            $('.navbar-default').removeClass('shrink');
        };
        if ($(document).scrollTop() > 320) {
            $('.navbar-brand').addClass('brand-shrink');
        } else {
            $('.navbar-brand').removeClass('brand-shrink');
        }
    });
    
    //toggle sidenav arrows up or down
    $('.panel-collapse').on('show.bs.collapse', function() {
        $(this).siblings('.panel-heading').addClass('active');
    });

    $('.panel-collapse').on('hide.bs.collapse', function() {
        $(this).siblings('.panel-heading').removeClass('active');
    });
    $('#collapse-overview').addClass('in');

    //homepage slider
    $("#carousel_home").carousel({ interval: 75000, pause: "hover" });

    //copy to clipboard
   var clipboard = new ClipboardJS('code');
    $('code').tooltip({
        trigger: 'click'
    });
});