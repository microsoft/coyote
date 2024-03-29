jQuery(document).ready(function ($) {

  //copy to clipboard
  var clipboard = new ClipboardJS('code');
  $('code').tooltip({
    trigger: 'click'
  });

  // make external links that start with http, and don't go to our own site, open in a new tab
  $('a[href^="http"]').not('a[href*="microsoft.github.io"]').not('a[href*="127.0.0.1"]').attr('target', '_blank');

  var year = (new Date()).getFullYear();
  $("#copyright").append("&copy; " + year + " Microsoft");

});
