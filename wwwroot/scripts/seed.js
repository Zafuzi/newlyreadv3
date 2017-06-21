$(function() {
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function() {
            navigator.serviceWorker.register('/sw.js').then(function(registration) {
                // Registration was successful
                console.log('ServiceWorker registration successful with scope: ', registration.scope);
            }).catch(function(err) {
                // registration failed :(
                console.log('ServiceWorker registration failed: ', err);
            });
        });
    }
    $("figure:has(> iframe)").css({
        "display": "none",
        "position": "relative",
        "height": 0,
        "width": "100%",
        "margin": 0,
        "padding": "25px 0 56.25% 0",
        "display": "flex",
    });
    $("iframe").css({
        "position": "absolute",
        "top": 0,
        "left": 0,
        "width": "100%",
        "height": "100%"
    })
    $("video").attr("controls", "true");


    $("header").stick_in_parent();
});