jQuery(document).ready(function ($) {

    // shrink nav onscroll - mobile first ux
    $(window).scroll(function () {
        if ($(document).scrollTop() > 20) {
            $('.navbar-default').addClass('shrink');
        } else {
            $('.navbar-default').removeClass('shrink');
        };
        if ($(document).scrollTop() > 320) {
            $('.brand-home').addClass('slide-out-top');

        } else {
            $('.brand-home').removeClass('slide-out-top').addClass('slide-in-top');
        }
    });

    //toggle sidenav arrows up or down
    $('.panel-collapse').on('show.bs.collapse', function () {
        $(this).siblings('.panel-heading').addClass('active');
    });

    $('.panel-collapse').on('hide.bs.collapse', function () {
        $(this).siblings('.panel-heading').removeClass('active');
    });

    //homepage slider
    $("#carousel_home").carousel({
        interval: 75000,
        pause: "hover"
    });

    //copy to clipboard
    var clipboard = new ClipboardJS('code');
    $('code').tooltip({
        trigger: 'click'
    });


    //trigger waypoint animations 

    $('#home_figure_1').waypoint(function () {
        $('#home_figure_1').addClass('slide-in-left');
    }, {
        offset: '70%'
    });    
    $('#home_text_1').waypoint(function () {
        $('#home_text_1').addClass('slide-in-right');
    }, {
        offset: '70%'
    });
    $('#home_figure_2a').waypoint(function () {
        $('#home_figure_2a').addClass('slide-in-right');
    }, {
        offset: '70%'
    });  
    $('#home_figure_2b').waypoint(function () {
        $('#home_figure_2b').addClass('slide-in-right');
    }, {
        offset: '70%'
    });     
    $('#home_text_2').waypoint(function () {
        $('#home_text_2').addClass('slide-in-left');
    }, {
        offset: '70%'
    });    
    $('#home_quote').waypoint(function () {
        $('#home_quote').addClass('puff-in-center');
    }, {
        offset: '70%'
    });      

});
