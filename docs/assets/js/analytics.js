// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

var siteConsent = null;
var telemetryInitialized = false;

function setupTelemetry() {
    // this is called from within the .wm-article iframe.
    if (!telemetryInitialized && window.top['ga-enable-UA-161403370-1']) {
        telemetryInitialized = true;

        // enable google analytics.
        window['ga-disable-UA-161403370-1'] = false;

        // see https://developers.google.com/analytics/devguides/collection/analyticsjs/
        (function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){
            (i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
            m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)
            })(window,document,'script','https://www.google-analytics.com/analytics.js','ga');

        var year = 365 * 24 * 60 * 60; // 1 year in seconds
        var flags = 'max-age=' + year + ';secure;samesite=none';
        ga('create', 'UA-161403370-1', {
            cookieFlags: flags
          });
        ga('send', 'pageview');
    }
}

function enableTelemetry() {
    window['ga-disable-UA-161403370-1'] = false;
    window['ga-enable-UA-161403370-1'] = true;
}

function disableTelemetry() {
    window['ga-disable-UA-161403370-1'] = true;
    window['ga-enable-UA-161403370-1'] = false;
}

function wcp_ready(err, _siteConsent){
    if (err != undefined) {
        return error;
    } else {
        siteConsent = _siteConsent;
        onConsentChanged();
    }
}

function onConsentChanged() {
    var userConsent = siteConsent.getConsentFor(WcpConsent.consentCategories.Analytics);
    if (!siteConsent.isConsentRequired) {
        enableTelemetry();
    }
    else if (userConsent) {
        enableTelemetry();
        var callback = window['telemetry-callback'];
        if (callback){
            callback();
        }
    }
    else {
        disableTelemetry()
    }
}

function manageCookies() {
    siteConsent.manageConsent();
    window.scroll({
        top: 0,
        left: 0,
        behavior: 'smooth'
      });
}

function initAnalytics() {
    WcpConsent.init("en-US", "cookie-banner", wcp_ready, onConsentChanged);
}

$(document).ready(function () {
    // $(document).ready triggers twice, once for the outer TOC and again for the iframe.
    // We only want one pageview event so we trigger on iframe since it has the
    // correct href location and not the funky '#' page reference which the TOC produces
    // as that would record everything as hitting the home page.
    if (window.top == window.self){
        telemetryInitialized = false;
        // show cookie banner in top window so it uses the outer cookie-banner
        initAnalytics();
    } else {
        window.top['telemetry-callback'] = setupTelemetry;
        // call it just in case consent was not required and telemetry is already enabled.
        setupTelemetry();
    };
});
