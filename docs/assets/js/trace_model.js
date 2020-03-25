var events = "";

function Event() {
    this.name = null;
    this.sender = null;
    this.senderState = null;
    this.receiver = null;
    this.receiverState = null;
}

function Actor() {
    this.name = null;
    this.type = null;
    this.currentState = "null";
    this.raisedEvent = false;
    this.inbox = new Array();

    this.enqueue = function(e) {
        this.inbox.push(e);
    }

    this.dequeue = function(){
        if (this.inbox.length > 0){
            var result = this.inbox[0];
            this.inbox.shift();
            return result;
        }
        return null;
    }
}

function Model() {
    this.events = new Array();
    this.map = new Array();

    this.addEvent = function addEvent(e){
        this.events.push(e);
    }

    this.getOrCreateActor = function(name, type)
    {
        var myMap = this.map;
        if (myMap[name] == undefined){
            var a = new Actor();
            a.name = name;
            a.type = type;
            myMap[name] = a;
        }
        return myMap[name];
    }
}

function getOrCreateActor(model, actor)
{
    var name = actor;
    var type = name;
    var pos = name.lastIndexOf('(');
    if (pos > 0) {
        type = name.substr(0, pos);
    }
    return model.getOrCreateActor(name, type);
}

function getShortName(fullName)
{
    var normalized = fullName.replace(/\+/g, ".")
    var i = normalized.lastIndexOf(".");
    if (i > 0) {
        // trim fully qualified name down to the short name for the state.
        return normalized.substr(i + 1);
    }
    return normalized;
}

function handleGotoState(model, step)
{
    // GotoState happens in response to something, like a previous SendEvent or RaiseEvent, which means the event info should be in our inbox.
    var state = step.getAttribute("newState");
    state = getShortName(state);
    var id = step.getAttribute("id");
    var source = getOrCreateActor(model, id);
    state = getStateName(id, state);
    var e = null;
    // raised events are special because they do not result in HandleDequeueEvent!
    if (source.raisedEvent)
    {
        e = source.dequeue();
        source.raisedEvent = false;
    }
    if (e != null)
    {
        e.receiver = source.name;
        e.receiverState = state;
        model.addEvent(e);
    }
    else
    {
        // must be the initial state.
        e = new Event();
        e.name = "init";
        e.receiver = source.name;
        e.receiverState = state;
        model.addEvent(e);
    }
    source.currentState = state;
}

function handleRaiseEvent(model, step)
{
    // event needs to capture transition on receiver side also, so for now this Event is incomplete,
    // so store it in the actor inbox for now.
    var eventName = step.getAttribute("event");
    var senderState = step.getAttribute("state");
    var id =step.getAttribute("id");
    var source = getOrCreateActor(model, id);
    source.raisedEvent = true;
    var e = new Event();
    e.name = eventName;
    e.sender = source.name;
    e.senderState = getStateName(id, senderState);
    e.receiver = source.name;
    source.enqueue(e);
}

function getStateName(sender, senderState)
{
    // actors have no sender state, so we make on up to match the DGML diagram.
    if (!senderState)
    {
        var type = sender;
        pos = type.lastIndexOf(".");
        if (pos > 0){
            return type.substr(pos + 1);
        }
        return type;
    }
    return senderState;
}

function handleSendEvent(model, step)
{
    // event needs to capture transition on receiver side also, so for now this Event is incomplete,
    // so store it in the actor inbox for now.
    var eventName = step.getAttribute("event");
    var sender = getSender(step);
    if (sender) {
        var source = getOrCreateActor(model, sender);
        var senderState = step.getAttribute("senderState");
        var target = getOrCreateActor(model, step.getAttribute("target"));
        var e = new Event();
        e.name = eventName;
        e.sender = source.name;
        e.senderState = getStateName(sender, senderState);
        e.receiver = target.name;
        target.enqueue(e);
    }
}

function handleDequeueEvent(model, step){
    // event is being handled by the target machine, so we now know the receiver state.
    var eventName = step.getAttribute("event");
    var state = step.getAttribute("state");
    var id = step.getAttribute("id");
    var source = getOrCreateActor(model, id);

    var e = source.dequeue();
    if (e != null) {
        e.receiverState = getStateName(id, state);
        model.addEvent(e);
    } else {
        console.log("### Empty queue on " + source.name + " on event " + eventName);
    }
}

function getMonitorId(step)
{
    var id = step.getAttribute("id");
    if (!id) {
        id = step.getAttribute("monitorType");
    }
    if (!id){
        console.log("error with monitor step: " + step.outerHTML);
    }
    return id;
}

function getSender(step){
    var name = step.getAttribute("sender");
    if (!name) {
        name = step.getAttribute("senderName");
    }
    if (!name) {
        name = "ExternalCode";
    }
    return name;
}

function handleMonitorState(model, step)
{
    // These seem to come in pairs, and show the state transitions that happen in the monitor.
    // In this case from  idle state to busy state.  We can use the inbox to unravel this.
    // <MonitorState id="Microsoft.Coyote.Samples.DrinksServingRobot.LivenessMonitor(1)" monitorType="Microsoft.Coyote.Samples.DrinksServingRobot.LivenessMonitor" state="Idle" isEntry="False" isInHotState="False" />
    // <MonitorState id="Microsoft.Coyote.Samples.DrinksServingRobot.LivenessMonitor(1)" monitorType="Microsoft.Coyote.Samples.DrinksServingRobot.LivenessMonitor" state="Busy" isEntry="True" isInHotState="True" />
    var monitor = getOrCreateActor(model, getMonitorId(step));
    var state = step.getAttribute("state");
    e = monitor.dequeue();
    if (e == null) {
        var e = new Event();
        e.name = "hidden";
        e.sender = monitor.name;
        e.senderState = state;
        e.receiver = monitor.name;
        monitor.enqueue(e);
    } else {
        e.receiverState = state;
        model.addEvent(e);
    }
}

function handleMonitorEvent(model, step)
{
    // <MonitorEvent sender="Microsoft.Coyote.Samples.DrinksServingRobot.Robot(7)" senderState="Active" id="Microsoft.Coyote.Samples.DrinksServingRobot.LivenessMonitor(1)" monitorType="LivenessMonitor" state="Idle" event="Microsoft.Coyote.Samples.DrinksServingRobot.LivenessMonitor+BusyEvent" />
    var source = getOrCreateActor(model, getSender(step));
    var senderState = step.getAttribute("senderState");
    var monitor = getOrCreateActor(model, getMonitorId(step));
    var state = step.getAttribute("state");
    var eventName = step.getAttribute("event");
    var e = new Event();
    e.name = eventName;
    e.sender = source.name;
    e.senderState = senderState;
    e.receiver = monitor.name;
    e.receiverState = state;
    // monitors don't actually have a queue, this is dequeued immediately...
    model.addEvent(e);
}

function handleErrorState(model, step)
{
    var source = getOrCreateActor(model, step.getAttribute("id"));
    var state = step.getAttribute("state");
    var e = new Event();
    e.name = "<error>";
    e.sender = source.name;
    e.senderState = state;
    e.receiver = source.name;
    e.receiverState = state;
    model.addEvent(e);
}

function convertTrace(doc) {
    var model = new Model();
    var ns = doc.documentElement.namespaceURI;
    var steps = doc.documentElement.childNodes;
    for (var i = 0; i < steps.length; i++)
    {
        var step = steps[i];
        if (step.nodeType == 1) {
            type = step.tagName;
            if (type == "Goto") {
                handleGotoState(model, step);
            } else if (type == "Raise") {
                handleRaiseEvent(model, step);
            } else if (type == "Send") {
                handleSendEvent(model, step);
            } else if (type == "DequeueEvent") {
                handleDequeueEvent(model, step);
            } else if (type == "MonitorState") {
                handleMonitorState(model, step);
            } else if (type == "MonitorEvent") {
                handleMonitorEvent(model, step);
            } else if (type == "ErrorState") {
                handleErrorState(model, step);
            }
        }
    }
    return model;
}

function fetchXml(url, asXml, handler)
{
    const xhr = new XMLHttpRequest();
    // listen for `onload` event
    xhr.onload = () => {
        // process response
        if (xhr.status == 200) {
            // parse JSON data
            if (asXml) {
                handler(xhr.responseXML);
            } else {
                handler(xhr.responseText);
            }
        } else {
            console.error('Error downloading:  ' + url + ", error=" + xhr.status);
        }
    };

    // create a `GET` request
    xhr.open('GET', url);

    // send request
    xhr.send();
}

jQuery(document).ready(function ($) {
    $(".animated_svg").each(function() {
        var div = $(this)[0];
        var xmltrace = $(this).attr("trace");
        var svgFile = $(this).attr("svg");
        if (xmltrace && svgFile) {
            fetchXml(svgFile, false, function (data){
                div.innerHTML = data;
                fetchXml(xmltrace, true, function (trace) {
                    model = convertTrace(trace);
                    start_trace(model.events, div);
                });
            });
        }
    });
});
