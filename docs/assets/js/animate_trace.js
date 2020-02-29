
// Include this script in an HTML page to animate the trace contained in the
// javascript json data element named "events", and display the results in the
// embedded SVG diagram.  This depends on progress_bar.js to show a progress UI.

var animate_events = null; // list of events to animate
var nodes = null;
var links = null;
var crossGroupLinks = null;
var position = 0;
var svg = null;
var currentStates = new Array();
var progressBar = null;
var playing = false;

const linkSeparator = "-\u003E";
const comet_color = "#409050";
const selected_node_color = "lightgreen";
const selected_node_foreground = "#3D3D3D";
const error_node_color = "#C15656";
const error_node_foreground = "#FFFFFF";
const comet_speed = 5; // 5 ms per step
const svgNS = "http://www.w3.org/2000/svg";
const restart_timeout = 5000; // 5 seconds
var normal_foreground = "#3D3D3D";

// see: http://owl3d.com/svg/vsw/articles/vsw_article.html
function start_trace(events) {
    animate_events = events;
    position = 0;
    start_animation();
}

ProgressBar = function(svg, options) {
    this.settings = {
        barColor : "#4F87AD",
        height: 12
    };
    if (options) {
        $(this.settings).extend(options);
    }
    var rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
    this.rect = rect;
    svg.appendChild(rect);
    rect.setAttribute("width", 0);
    rect.setAttribute("height", this.settings.height);
    var height = svg.height.baseVal.value;
    var width =  svg.width.baseVal.value;
    var scaleX = 1;
    var scaleY = 1;
    if (svg.viewBox){
        viewBoxHeight = svg.viewBox.baseVal.height;
        viewBoxWidth = svg.viewBox.baseVal.width;
        scaleX = viewBoxWidth / width;
        width = viewBoxWidth;
        scaleY = viewBoxHeight / height;
        height = viewBoxHeight;
    }
    rect.setAttribute("y", height - this.settings.height);
    rect.setAttribute("fill", this.settings.barColor);

    var buttonSize = 16 * scaleX;
    var button = document.createElementNS("http://www.w3.org/2000/svg", "ellipse");
    svg.appendChild(button);
    var penWidth = 2 / scaleX;
    var centerX = width - buttonSize - penWidth;
    var centerY = height - buttonSize - penWidth
    button.setAttribute("cx", centerX);
    button.setAttribute("cy", centerY);
    button.setAttribute("rx", buttonSize);
    button.setAttribute("ry", buttonSize);
    button.setAttribute("fill", "white");
    button.setAttribute("stroke", this.settings.barColor);
    button.setAttribute("stroke-width", 2 * scaleX);
    button.progress = this;

    var triangleSize = 10 * scaleX;
    var triangle = document.createElementNS("http://www.w3.org/2000/svg", "polygon");
    svg.appendChild(triangle);
    var points = [[centerX - triangleSize / 2, centerY - triangleSize / 2],
                [centerX + triangleSize / 2, centerY],
                [centerX - triangleSize / 2, centerY + triangleSize / 2]]
    triangle.setAttribute("points", points.toString());
    triangle.setAttribute("fill", this.settings.barColor);
    triangle.setAttribute("stroke", this.settings.barColor);
    triangle.setAttribute("stroke-width", 2 * scaleX);
    triangle.setAttribute("opacity", 0);
    triangle.progress = this;

    var pauseHeight = 14 * scaleX;
    var pauseWidth = 4 * scaleX;
    var pauseGap = 2 * scaleX;
    var pause = document.createElementNS("http://www.w3.org/2000/svg", "g");
    svg.appendChild(pause);
    var leftRect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
    pause.appendChild(leftRect);
    var rightRect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
    pause.appendChild(rightRect);

    leftRect.setAttribute("x", centerX - pauseWidth - pauseGap);
    leftRect.setAttribute("y", centerY - pauseHeight / 2);
    leftRect.setAttribute("width", pauseWidth);
    leftRect.setAttribute("height", pauseHeight);
    leftRect.setAttribute("fill", this.settings.barColor);
    leftRect.setAttribute("stroke", this.settings.barColor);
    leftRect.setAttribute("stroke-width", 2 * scaleX);
    leftRect.setAttribute("opacity", 0);
    leftRect.progress = this;

    rightRect.setAttribute("x", centerX + pauseGap);
    rightRect.setAttribute("y", centerY - pauseHeight / 2);
    rightRect.setAttribute("width", pauseWidth);
    rightRect.setAttribute("height", pauseHeight);
    rightRect.setAttribute("fill", this.settings.barColor);
    rightRect.setAttribute("stroke", this.settings.barColor);
    rightRect.setAttribute("stroke-width", 2 * scaleX);
    rightRect.setAttribute("opacity", 0);
    rightRect.progress = this;

    this.setProgress = function (percent) {
        this.rect.setAttribute("width", "" + percent + "%");
    }

    this.handleClick = function(e) {
        if (this.progress.onclick) {
            this.progress.onclick();
        }
    };

    button.onclick = this.handleClick;
    triangle.onclick = this.handleClick;
    leftRect.onclick = this.handleClick;
    rightRect.onclick = this.handleClick;

    this.setState = function (state) {
        if (state) {
            // playing, so show pause button
            leftRect.setAttribute("opacity", 1);
            rightRect.setAttribute("opacity", 1);
            triangle.setAttribute("opacity", 0);
        } else {
            // stopped, so show play button
            leftRect.setAttribute("opacity", 0);
            rightRect.setAttribute("opacity", 0);
            triangle.setAttribute("opacity", 1);
        }
    }

    this.setState(1);
}

function start_animation() {
    playing = true;
    tags = document.getElementsByTagNameNS(svgNS, "svg");
    if (tags.length > 0) {
        svg = tags[0];
        if (!progressBar) {
            progressBar = new ProgressBar(svg);
            progressBar.onclick = function (state) {
                if (playing) {
                    stop_animation();
                } else {
                    start_animation();
                }
            }
        }
        progressBar.setState(1);
        if (!crossGroupLinks) {
            nodes = new Array();
            links = new Array();
            crossGroupLinks = new Array();
            find_nodes(svg.children, 0);
            hide_crossGroupLinks_links(crossGroupLinks);
            trim_bad_events();
        }
    }
    if (position >= animate_events.length) {
        deselectAll();
        position = 0;
        hide_crossGroupLinks_links(crossGroupLinks);
    }
    window.setTimeout(animate, 100);
}

function stop_animation() {
    playing = false;
    if (progressBar) {
        progressBar.setState(0);
    }
}

function find_nodes(children, depth) {
    if (children != null && children.length > 0) {
        for (var i = 0; i < children.length; i++) {
            c = children[i];
            if (c.tagName == "g") {
                newDepth = depth;
                if (c.id) {
                    if (c.id.includes(linkSeparator)) {
                        links[c.id] = c;
                        if (depth == 0) {
                            crossGroupLinks[c.id] = c;
                        }
                    }
                    else {
                        nodes[c.id] = c;
                    }
                    newDepth++;
                }
                if (c.children.length > 0) {
                    find_nodes(c.children, newDepth);
                }
            }
        }
    }
}

function hide_crossGroupLinks_links(map) {
    pos = 0;
    for (var i in map) {
        g = map[i]
        if (g.id.includes(linkSeparator)) {
            g.style.display = "none";
        }
    }
}

var animatingEvent = null;

function trim_bad_events() {
    // remove events for which we have no SVG geometry to animate.
    var trimmed = new Array();
    for (var i = 0; i < animate_events.length; i++)
    {
        e = animate_events[i];
        if (e.sender) {
            linkId = e.sender + "." + e.senderState + "->" + e.receiver + "." + e.receiverState;
            link = links[linkId];
            if (link || e.name == "<error>"){
                trimmed.push(e);
            } else {
                console.log("???" + linkId);
            }
        }
    }
    animate_events = trimmed;
}

function animate() {
    if (position < animate_events.length && playing) {
        e = animate_events[position++];
        progressBar.setProgress(position * 100 / animate_events.length);
        animatingLink = false;
        if (e.sender)
        {
            // then we can animate a link.
            linkId = e.sender + "." + e.senderState + "->" + e.receiver + "." + e.receiverState;
            link = links[linkId];
            if (link) {
                if (link.children[0]) {
                    animatingEvent = e;
                    link.style.display = "";
                    start_animate_path(link.children[0]);
                    animatingLink = true;
                    if (currentStates[e.sender] == undefined) {
                        selectNode(e.sender, e.senderState, selected_node_color, selected_node_foreground);
                    }
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
                selectNode(e.sender, e.senderState, error_node_color, error_node_foreground);
            }
        }
        if (!animatingLink){
            window.setTimeout(animate, 100);
        }
    } else {
        stop_animation();
    }
}

function deselectAll()
{
    for(var groupId in currentStates){
        selected = currentStates[groupId];
        if (selected){
            selected[0].setAttribute("fill", "white");
            selected[1].setAttribute("fill", normal_foreground);
        }
    }
}

function selectNode(groupId, nodeId, node_color, foreground)
{
    if (groupId) {
        nodeId = groupId + "." + nodeId;
        n = nodes[nodeId]
        if (n){
            rect = n.children[0];
            text = n.children[1];
            selected = currentStates[groupId];
            if (selected){
                selected[0].setAttribute("fill", "white");
                selected[1].setAttribute("fill", normal_foreground);
            }
            currentStates[groupId] = [rect, text];
            rect.setAttribute("fill", node_color);
            text.setAttribute("fill", foreground);
        }
    }
}

var animatingPath = null;
var pathLength = 0;
var pathPosition = 0.0;
var pathStarts = [];
var pathEnd = 0.0;
var pathStep = 0.0;
var comets = null;

function start_animate_path(path) {
    comets = new Array();
    animatingPath = path;
    pathLength = path.getTotalLength();
    pathPosition = 0.0;
    pathStep = pathLength / 50;
    for (var i = 0; i < 8; i += 1)
    {
        var comet = document.createElementNS(svgNS, "path");
        var points = path.getAttribute("d");
        comet.setAttribute("d", points);
        comet.style.strokeWidth = i + 1;
        comet.style.stroke = comet_color;
        comet.style.fill = "none";
        comet.style.strokeDasharray =  [ 15 - i, pathLength + 15];
        pathEnd =  (pathLength + 15);
        pathStarts[i] = (15 - i);
        comet.style.strokeDashoffset = pathStarts[i];
        root = svg.children[2];
        root.appendChild(comet);
        comets[i] = comet;
    }
    window.setTimeout(animate_path, comet_speed);
}

function animate_path() {
    pathPosition += pathStep;
    if (pathPosition > pathEnd) {
        root = svg.children[2];
        for (var i in comets) {
            root.removeChild(comets[i]);
        }
        comets = null;
        console.log(animatingEvent.receiver + "." + animatingEvent.receiverState);
        selectNode(animatingEvent.receiver, animatingEvent.receiverState, selected_node_color, selected_node_foreground);
        window.setTimeout(animate, 30);
    } else {
        var x = position * 100 / animate_events.length;
        var y = (position + 1) * 100 / animate_events.length;
        var offset = (y - x) * pathPosition / pathEnd;
        progressBar.setProgress(x + offset);

        for (var i in comets) {
            comet = comets[i];
            comet.style.strokeDashoffset = pathStarts[i] - pathPosition - i;
        }
        window.setTimeout(animate_path, comet_speed);
    }
}
