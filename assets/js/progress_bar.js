// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

class ProgressBar {

    rect = null;
    onplay = null;
    onpause = null;
    onfullscreen = null;
    onnormalscreen = null;
    pauseButton = null;
    playButton = null;
    fastforwardButton = null;
    fullScreenButton = null;
    normalScreenButton = null;
    playerContent = null;

    constructor(playerContent) {
        this.rect = $("#progress-rect")[0];
        this.playerContent = playerContent;

        this.pauseButton = $(".pause-button")[0];
        this.playButton = $(".play-button")[0];
        this.fastforwardButton = $(".fastforward-button")[0];
        this.fullScreenButton = $(".fullscreen-button")[0];
        this.normalScreenButton = $(".normal-screen-button")[0];

        var foo = this;
        this.pauseButton.onclick = function(e) { foo.pause(e); }
        this.playButton.onclick = function(e) { foo.play(e); }
        this.fastforwardButton.onclick = function(e) { foo.fast_forward(e);}
        this.fullScreenButton.onclick = function(e) { foo.fullscreen(e); }
        this.normalScreenButton.onclick = function(e) { foo.normalscreen(e); }

        this.setPlaying(0);
        this.setFullscreen(0);

        $(window).resize(function(){
            foo.handle_resize();
        });
        foo.handle_resize();
    }

    handle_resize() {
        if (this.playerContent != null) {
            var w = this.playerContent.clientWidth - 150; // leave room for buttons
            this.rect.parentNode.setAttribute("width", w);
        }
    }

    setProgress(percent) {
        this.rect.setAttribute("width", "" + percent + "%");
    }

    play(e) {
        if (this.onplay) {
            this.onplay(e);
        }
    };

    fast_forward(e) {
        if (this.onfast) {
            this.onfast(e);
        }
    }

    pause(e) {
        if (this.onpause) {
            this.onpause(e);
        }
    };

    fullscreen(e) {
        if (this.onfullscreen) {
            this.onfullscreen(e);
            this.handle_resize();
            this.setFullscreen(true);
        }
    };

    normalscreen(e) {
        if (this.onnormalscreen) {
            this.onnormalscreen(e);
            this.handle_resize();
            this.setFullscreen(false);
        }
    };

    setPlaying(state) {
        if (state) {
            // playing, so show pause button
            this.pauseButton.style.display = "inline";
            this.playButton.style.display = "none";
        } else {
            // stopped, so show play button
            this.pauseButton.style.display = "none";
            this.playButton.style.display = "inline";
        }
    }

    setFullscreen(state) {
        if (state) {
            // is full screen, so show normal screen button
            this.normalScreenButton.style.display = "inline";
            this.fullScreenButton.style.display = "none";
        } else {
            // normal window size, so show fullscreen button
            this.normalScreenButton.style.display = "none";
            this.fullScreenButton.style.display = "inline";
        }
    }
}
