
// Include this script in an HTML page to animate the trace contained in the
// javascript json data element named "events", and display the results in the
// embedded SVG diagram.
var animate_events = null; // list of events to animate
var nodes = null;
var links = null;
var crossGroupLinks = null;
var position = 0;
var svg = null;
var currentStates = new Array();

const linkSeparator = "-\u003E";
const comet_color = "#409050";
const selected_node_color = "lightgreen";
const comet_speed = 5; // 5 ms per step
const svgNS = "http://www.w3.org/2000/svg";

// todo: integrate wormify: http://owl3d.com/svg/vsw/articles/vsw_article.html
function start_trace(events) {
    animate_events = events;
    start_animation();
}

function start_animation() {
    tags = document.getElementsByTagNameNS(svgNS, "svg");
    if (tags.length > 0) {
        svg = tags[0];
        nodes = new Array();
        links = new Array();
        crossGroupLinks = new Array();
        find_nodes(svg.children, 0);
        hide_crossGroupLinks_links(crossGroupLinks);
    }
    deselectAll();
    position = 0;
    window.setTimeout(animate, 100);
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

function animate() {
    if (position < animate_events.length) {
        e = animate_events[position++];
        animatingLink = false;
        if (e.sender)
        {
            // then we can animate a link.
            linkId = e.sender + "." + e.senderState + "->" + e.receiver + "." + e.receiverState;
            link = links[linkId];
            if (link){
                animatingEvent = e;
                link.style.display = "";
                start_animate_path(link.children[0]);
                animatingLink = true;
                if (currentStates[e.sender] == undefined) {
                    selectNode(e.sender, e.senderState);
                }
            }
            else{
                console.log("???" + linkId);
            }
        }
        if (!animatingLink){
            window.setTimeout(animate, 100);
        }
    } else {
        window.setTimeout(start_animation, 2000);
    }
}

function deselectAll()
{
    for(var groupId in currentStates){
        selected = currentStates[groupId];
        if (selected){
            selected.setAttribute("fill", "white");
        }
    }
}

function selectNode(groupId, nodeId)
{
    if (groupId) {
        nodeId = groupId + "." + nodeId;
        n = nodes[nodeId]
        if (n){
            rect = n.children[0];
            selected = currentStates[groupId];
            if (selected){
                selected.setAttribute("fill", "white");
            }
            currentStates[groupId] = rect;
            rect.setAttribute("fill", selected_node_color);
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
        selectNode(animatingEvent.receiver, animatingEvent.receiverState);
        window.setTimeout(animate, 30);
    } else {
        for (var i in comets) {
            comet = comets[i];
            comet.style.strokeDashoffset = pathStarts[i] - pathPosition - i;
        }
        window.setTimeout(animate_path, comet_speed);
    }
}
