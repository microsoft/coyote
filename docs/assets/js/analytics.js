
var awaInitialized = false;

function addTelemetryTag() {
    if (!awaInitialized) {
        awaInitialized = true;
        awaConfig.userConsented = true;
        awa.init(awaConfig);
    }
}

function hideBanner(){
    $(".cookie-banner").hide();
}

// cookie banner callbacks, if consent is enabled, then we can enable the analytics.
if (typeof (mscc) === 'undefined' || mscc.hasConsent()) {
    addTelemetryTag();
    hideBanner();
} else {
    mscc.on('consent', addTelemetryTag);
    mscc.on('hide', hideBanner);
}
