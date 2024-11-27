// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Include this script in an HTML page to animate the trace contained in the
// javascript json data element named "events", and display the results in the
// embedded SVG diagram.  This depends on progress_bar.js to show a progress UI.

class CometPath {
    animatingEvent = null;
    animatingPath = null;
    pathLength = 0;
    pathPosition = 0.0;
    pathStarts = [];
    pathEnd = 0.0;
    pathStep = 0.0;
    pathChunk = 30;
    comets = null;
    svgNS = "http://www.w3.org/2000/svg";
    completed = null; // event callback.
    progress = null; // event callback (given progress between 0 and 1)
    comet_color = "#90EE90";

    start_animate_path(svg, e, path, speed) {
        this.animatingEvent = e;
        this.comet_speed = speed
        this.svg = svg;
        var defs = svg.children[0];
        if (defs.tagName != "defs"){
            defs = document.createElementNS(xmlns, "defs");
            svg.appendChild(defs);
        }
        if (defs.children.length == 0){
            var svgfilter = document.getElementById("svgfilter");
            var filterdefs = svgfilter.children[0];
            var filter = filterdefs.children[0];
            filter.remove();
            defs.appendChild(filter);
        }
        this.comets = new Array();
        this.animatingPath = path;
        this.pathLength = path.getTotalLength();
        this.pathPosition = 0.0;
        this.pathStep = this.pathLength / this.pathChunk;
        this.startTime = new Date();
        for (var i = 0; i < 8; i += 1)
        {
            var comet = document.createElementNS(this.svgNS, "path");
            var points = path.getAttribute("d");
            comet.setAttribute("d", points);
            comet.style.strokeWidth = i + 1;
            comet.style.stroke = this.comet_color;
            comet.style.fill = "none";
            comet.style.filter = "url(#green-glow)";
            comet.style.strokeDasharray =  [ 15 - i, this.pathLength + 15];
            this.pathEnd =  (this.pathLength + 15);
            this.pathStarts[i] = (15 - i);
            comet.style.strokeDashoffset = this.pathStarts[i];
            var root = this.svg.children[2];
            root.appendChild(comet);
            this.comets[i] = comet;
        }

        var foo = this;
        window.setTimeout(function() { foo.animate_path() }, this.comet_speed);
    }

    animate_path() {
        this.pathPosition += this.pathStep;
        if (this.pathPosition > this.pathEnd) {
            var root = this.svg.children[2];
            for (var i in this.comets) {
                root.removeChild(this.comets[i]);
            }
            this.comets = null;
            if (this.completed) {
                this.completed();
            }
        } else {
            var now = new Date();
            var difference = new Date();
            difference.setTime(now.getTime() - this.startTime.getTime());
            var milliseconds = difference.getMilliseconds();
            var speed = Math.ceil(this.pathLength / milliseconds);
            if (speed < this.comet_speed && this.pathChunk > 10) {
                this.pathChunk -= 10;
            }
            else if (speed > this.comet_speed)
            {
                this.pathChunk += 10;
            }

            if (this.progress != null)
            {
                this.progress(this.pathPosition / this.pathEnd);
            }

            for (var i in this.comets) {
                var comet = this.comets[i];
                comet.style.strokeDashoffset = this.pathStarts[i] - this.pathPosition - i;
            }

            var foo = this;
            window.setTimeout(function() { foo.animate_path() }, this.comet_speed);
        }
    }

}

class AnimateTrace {
    animate_events = null; // list of events to animate
    nodes = null;
    links = null;
    crossGroupLinks = null;
    position = 0;
    currentStates = new Array();
    progressBar = null;
    playing = false;
    svg = null;
    parallel = false;

    linkSeparator = "-\u003E";
    selected_node_color = "lightgreen";
    selected_node_foreground = "#3D3D3D";
    error_node_color = "#C15656";
    error_node_foreground = "#FFFFFF";
    comet_speed = 5; // 5 ms per step
    restart_timeout = 5000; // 5 seconds
    normal_foreground = "#3D3D3D";

    constructor(parentDiv){
        this.parentDiv = parentDiv;
        if (window.location.search == "?parallel") {
            this.parallel = true;
        }
        if (!this.progressBar) {
            this.progressBar = new ProgressBar(parentDiv);
            var foo = this;
            this.progressBar.onplay = function (e) { foo.handle_start(e, false); };
            this.progressBar.onfast = function (e) { foo.handle_start(e, true); };
            this.progressBar.onpause = function (e) { foo.handle_stop(e, false); };
            this.progressBar.onfullscreen = function (e) { foo.handle_fullscreen(e); };
            this.progressBar.onnormalscreen = function (e) { foo.handle_normalscreen(e); };
            document.addEventListener('keydown', function(e) { foo.handle_key_down(e); });
        }
    }

    handle_key_down(e) {
        if (e.code == "F8"){
            this.progressBar.play(e);
        }
    }

    // see: https://owl3d.com/svg/vsw/articles/vsw_article.html
    start_trace(events) {
        this.position = 0;
        this.animate_events = events;
        this.svg = $(this.parentDiv).children("svg");
        if (this.svg.length > 0)
        {
            this.svg = this.svg[0];
            this.start_animation(this.svg);
        }
    }

    handle_fullscreen(e) {
        $(".wm-page-content").css('max-width', 'unset');
    }

    handle_normalscreen(e) {
        $(".wm-page-content").css('max-width', '800px');
    }

    handle_start(e, parallel) {
        this.parallel = parallel;
        this.start_animation(this.svg);
    }

    handle_stop(e, parallel){
        this.stop_animation();
    }

    start_animation(svg) {
        this.playing = true;
        this.progressBar.setPlaying(1); // play automatically

        if (!this.crossGroupLinks) {
            this.nodes = new Array();
            this.links = new Array();
            this.crossGroupLinks = new Array();
            this.find_nodes(svg.children, 0);
            this.hide_crossGroupLinks_links(this.crossGroupLinks);
            this.trim_bad_events();
        }

        if (this.position >= this.animate_events.length) {
            this.deselectAll();
            this.position = 0;
            this.hide_crossGroupLinks_links(this.crossGroupLinks);
        }

        var foo = this;
        window.setTimeout(function() { foo.animate() }, 100);
    }

    stop_animation() {
        this.playing = false;
        if (this.progressBar) {
            this.progressBar.setPlaying(0);
        }
    }

    find_nodes(children, depth) {
        if (children != null && children.length > 0) {
            for (var i = 0; i < children.length; i++) {
                var c = children[i];
                if (c.tagName == "g") {
                    var newDepth = depth;
                    if (c.id) {
                        if (c.id.includes(this.linkSeparator)) {
                            this.links[c.id] = c;
                            if (depth == 0) {
                                this.crossGroupLinks[c.id] = c;
                            }
                        }
                        else {
                            this.nodes[c.id] = c;
                        }
                        newDepth++;
                    }
                    if (c.children.length > 0) {
                        this.find_nodes(c.children, newDepth);
                    }
                }
            }
        }
    }

    hide_crossGroupLinks_links(map) {
        // this.pos = 0;??
        for (var i in map) {
            var g = map[i]
            if (g.id.includes(this.linkSeparator)) {
                g.style.display = "none";
            }
        }
    }

    trim_bad_events() {
        // remove events for which we have no SVG geometry to animate.
        var trimmed = new Array();
        for (var i = 0; i < this.animate_events.length; i++)
        {
            var e = this.animate_events[i];
            if (e.sender) {
                var linkId = e.sender + "." + e.senderState + "->" + e.receiver + "." + e.receiverState;
                var link = this.links[linkId];
                if (link || e.name == "<error>"){
                    trimmed.push(e);
                } else {
                    console.log("???" + linkId);
                }
            }
        }
        this.animate_events = trimmed;
    }

    animate() {
        if (this.position < this.animate_events.length && this.playing) {
            var e = this.animate_events[this.position++];
            this.progressBar.setProgress(this.position * 100 / this.animate_events.length);
            var animatingLink = false;
            if (e.sender)
            {
                // then we can animate a link.
                var linkId = e.sender + "." + e.senderState + "->" + e.receiver + "." + e.receiverState;
                var link = this.links[linkId];
                if (link) {
                    if (link.children[0]) {
                        var animatingPath = new CometPath();
                        animatingPath.start_animate_path(this.svg, e, link.children[0], this.comet_speed);
                        link.style.display = "";
                        if (!this.parallel) {
                            animatingLink = true;
                        }
                        if (this.currentStates[e.sender] == undefined) {
                            this.selectNode(e.sender, e.senderState, this.selected_node_color, this.selected_node_foreground);
                        }

                        var foo = this;
                        animatingPath.completed = function() { foo.onPathCompleted(animatingPath.animatingEvent); };
                        animatingPath.progress = function (p) { foo.showProgress(p); };
                    } else {
                        console.log("??? no children: " + linkId);
                    }
                }
                else {
                    console.log("???" + linkId);
                }
                if (e.name == "<error>")
                {
                    // then this event is about showing an error
                    this.selectNode(e.sender, e.senderState, this.error_node_color, this.error_node_foreground);
                }
            }

            if (!animatingLink) {
                var foo = this;
                window.setTimeout(function() { foo.animate() }, 100);
            }
        } else {
            this.stop_animation();
        }
    }

    onPathCompleted(e) {
        console.log(e.receiver + "." + e.receiverState);
        this.selectNode(e.receiver, e.receiverState, this.selected_node_color, this.selected_node_foreground);

        // start the next one when this link finishes.
        var foo = this;
        window.setTimeout(function() { foo.animate() }, 30);
    }

    showProgress(percent){
        var x = this.position * 100 / this.animate_events.length;
        var y = (this.position + 1) * 100 / this.animate_events.length;
        var offset = (y - x) * percent;
        this.progressBar.setProgress(x + offset);
    }

    deselectAll() {
        for(var groupId in this.currentStates){
            var selected = this.currentStates[groupId];
            if (selected){
                selected[0].setAttribute("fill", "white");
                selected[1].setAttribute("fill", this.normal_foreground);
            }
        }
    }

    selectNode(groupId, nodeId, node_color, foreground) {
        if (groupId) {
            var nodeId = groupId + "." + nodeId;
            var n = this.nodes[nodeId]
            if (n) {
                var rect = n.children[0];
                var text = n.children[1];
                var selected = this.currentStates[groupId];
                if (selected) {
                    selected[0].setAttribute("fill", "white");
                    selected[1].setAttribute("fill", this.normal_foreground);
                }
                this.currentStates[groupId] = [rect, text];
                rect.setAttribute("fill", node_color);
                text.setAttribute("fill", foreground);
            }
        }
    }

}
