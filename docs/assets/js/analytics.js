// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

var siteConsent = null;
var telemetryInitialized = false;

function enableTelemetry() {
    if (!telemetryInitialized) {
        telemetryInitialized = true;

        // enable google analytics.
        window['ga-disable-UA-161403370-1'] = false;

        // see https://developers.google.com/analytics/devguides/collection/analyticsjs/
        (function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){
            (i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
            m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)
            })(window,document,'script','https://www.google-analytics.com/analytics.js','ga');

        ga('create', 'UA-161403370-1', 'auto');
        ga('send', 'pageview');
    }
}

function disableTelemetry() {
    if (telemetryInitialized) {
        telemetryInitialized = false;
        window['ga-disable-UA-161403370-1'] = true;
    }
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

$(document).ready(function () {
    WcpConsent.init("en-US", "cookie-banner", wcp_ready, onConsentChanged);
});
