
const actorNS = "http://schemas.datacontract.org/2004/07/Microsoft.Coyote.Actors";
const runtimeNS = "http://schemas.datacontract.org/2004/07/Microsoft.Coyote.Runtime";
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
    this.currentState = null;
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
    var name = actor.getElementsByTagNameNS(actorNS, "Name")[0].textContent;
    var type = actor.getElementsByTagNameNS(actorNS, "Type")[0].textContent;
    return model.getOrCreateActor(name, type);
}

function handleGotoState(model, step)
{
    var ns = step.namespaceURI;
    // GotoState happens in response to something, like a previous SendEvent or RaiseEvent, which means the event info should be in our inbox.
    var state = step.getElementsByTagNameNS(ns, "MachineState")[0].textContent;
    var source = getOrCreateActor(model, step.getElementsByTagNameNS(ns, "SourceId")[0]);
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
    var ns = step.namespaceURI;
    var eventInfo = step.getElementsByTagNameNS(ns, "EventInfo")[0];
    var eventName = eventInfo.getElementsByTagNameNS(runtimeNS, "EventName")[0].textContent;
    var senderState = step.getElementsByTagNameNS(ns, "MachineState")[0].textContent;
    var source = getOrCreateActor(model, step.getElementsByTagNameNS(ns, "SourceId")[0]);
    source.raisedEvent = true;
    var e = new Event();
    e.name = eventName;
    e.sender = source.name;
    e.senderState = senderState;
    e.receiver = source.name;
    source.enqueue(e);
}

function handleSendEvent(model, step)
{
    // event needs to capture transition on receiver side also, so for now this Event is incomplete,
    // so store it in the actor inbox for now.
    var ns = step.namespaceURI;
    var eventInfo = step.getElementsByTagNameNS(ns, "EventInfo")[0];
    var eventName = eventInfo.getElementsByTagNameNS(runtimeNS, "EventName")[0].textContent;
    var senderState = step.getElementsByTagNameNS(ns, "MachineState")[0].textContent;
    var source = getOrCreateActor(model, step.getElementsByTagNameNS(ns, "SourceId")[0]);
    var target = getOrCreateActor(model, step.getElementsByTagNameNS(ns, "TargetId")[0]);
    var e = new Event();
    e.name = eventName;
    e.sender = source.name;
    e.senderState = senderState;
    e.receiver = target.name;
    target.enqueue(e);
}

function handleDequeueEvent(model, step){
    // event is being handled by the target machine, so we now know the receiver state.
    var ns = step.namespaceURI;
    var eventInfo = step.getElementsByTagNameNS(ns, "EventInfo")[0];
    var eventName = eventInfo.getElementsByTagNameNS(runtimeNS, "EventName")[0].textContent;
    var state = step.getElementsByTagNameNS(ns, "MachineState")[0].textContent;
    var source = getOrCreateActor(model, step.getElementsByTagNameNS(ns, "SourceId")[0]);

    var e = source.dequeue();
    if (e != null) {
        e.receiverState = state;
        model.addEvent(e);
    } else {
        console.log("### Empty queue on " + source.name + " on event " + eventName);
    }
}

function convertTrace(doc) {
    var model = new Model();
    var ns = doc.documentElement.namespaceURI;
    var steps = doc.documentElement.getElementsByTagNameNS(ns, "Steps")[0];
    var children = steps.getElementsByTagNameNS(ns, "BugTraceStep");
    for (var i = 0; i < children.length; i++)
    {
        var step = children[i];
        var type = step.getElementsByTagNameNS(ns, "Type");
        if (type != null && type.length > 0) {
            type = type[0].textContent;
            if (type == "GotoState") {
                handleGotoState(model, step);
            } else if (type == "RaiseEvent"){
                handleRaiseEvent(model, step);
            } else if (type == "SendEvent"){
                handleSendEvent(model, step);
            } else if (type == "DequeueEvent") {
                handleDequeueEvent(model, step);
            }
        }
    }

    start_trace(model.events);
}

function fetchTrace(url, handler)
{
    const xhr = new XMLHttpRequest();
    // listen for `onload` event
    xhr.onload = () => {
        // process response
        if (xhr.status == 200) {
            // parse JSON data
            var trace = xhr.responseXML;
            handler(trace);
        } else {
            console.error('Error downloading:  ' + url + ", error=" + xhr.status);
        }
    };

    // create a `GET` request
    xhr.open('GET', url);

    // send request
    xhr.send();
}
