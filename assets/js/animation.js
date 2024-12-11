// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This script provides the hero animation

class Dimensions {
    width = 0;
    height = 0;
};

class PathInfo {
    path = null;
    length = 0;
    offset = 0;
    step = 0;
    opacity = 1;
    stage = 0;
    dots = []
};

class LabelInfo {
    label = null;
    fade = false;
};

class HeroAnimation {
    NUMLAYERS = 12;
    layerMap = [17,4,2,4,3,5,6,7,3,5,10];
    layers = {};
    svg = null;
    bugColor = "#F80269";
    activeColor = "#06C1C5";
    testColor = "white";
    stageTimes = [1, 16000, 10000];
    stage = 0;
    labels = ["A typical distributed system", "Coyote tests these systems", "Coyote can find bugs and reproduce them"];
    currentLabel = null;
    maxParallel = 5;
    totalPaths = 0;
    totalReplays = 0;
    backgroundStarSize = 3;
    backgroundStarColor = "#142D38";
    mediumStarSize = 5;
    mediumStarColor = "#06C1C5";
    foregroundStarSize = 20;
    svg = null;

    start(svg) {

        this.svg = svg;
        this.NUMLAYERS = this.layerMap.length;

        // background stars just for effect
        for (var i = 0; i < 500; i++){
            this.addRandomStar(svg, this.backgroundStarSize, this.backgroundStarColor);
        }
        for (var i = 0; i < 200; i++){
            this.addRandomStar(svg, this.mediumStarSize, this.mediumStarColor);
        }

        // add layers
        for (var i = 0; i < this.NUMLAYERS; i++){
            var num = this.layerMap[i];
            if (i ==0 ) {
                num = 1; // first layer must be only one node
            }
            this.addLayer(svg, i, num, this.foregroundStarSize, this.activeColor);
        }

        window.setTimeout(function() { hero_animation.nextStage() }, this.stageTimes[this.stage]);
    }

    addRandomStar(svg, size, color){
        var margin = size * 2;
        var dims = this.getDimensions(svg);
        var maxHeight = dims.height - 2 * margin;
        var maxWidth =  dims.width - 2 * margin;
        this.addStar(svg, margin + Math.random() * maxWidth, margin + Math.random() * maxHeight, size, color);
    }

    getDimensions(svg) {
        var maxHeight = svg.height.baseVal.value;
        var maxWidth =  svg.width.baseVal.value;
        if (svg.viewBox){
            maxHeight = svg.viewBox.baseVal.height;
            maxWidth = svg.viewBox.baseVal.width;
        }
        var dims = new Dimensions();
        dims.width = maxWidth;
        dims.height = maxHeight;
        return dims;
    }

    fadeToLabel(info) {
        if (info.fade){
            info.opacity -= 0.03;
            if (info.opacity < 0){
                this.currentLabel.innerHTML = info.label;
                info.fade = false;
            }
            window.setTimeout(function(e) { hero_animation.fadeToLabel(e) }, 30, info);
        } else {
            info.opacity += 0.03;
            if (info.opacity > 1)
            {
                info.opacity = 1;
            } else {
                window.setTimeout(function(e) { hero_animation.fadeToLabel(e) }, 30, info);
            }
        }
        this.currentLabel.style.fillOpacity = info.opacity;
    }

    nextStage() {
        if (this.stage < this.labels.length)
        {
            var label = this.labels[this.stage];
            if (this.currentLabel == null) {
                var dims = this.getDimensions(svg);
                this.currentLabel = document.createElementNS("http://www.w3.org/2000/svg", "text");
                this.currentLabel.setAttribute("class", "title");
                this.currentLabel.innerHTML = label;
                this.currentLabel.setAttribute("x", 50);
                this.currentLabel.setAttribute("y", dims.height - 70);
                svg.appendChild(this.currentLabel);
            } else {
                var info = new LabelInfo();
                info.label = label;
                info.fade = true;
                info.opacity = 1;
                window.setTimeout(function(e) { hero_animation.fadeToLabel(e) }, 30, info);
            }
        }
        this.stage++;
        if (this.stage == 1)
        {
            // kick off creating paths.
            window.setTimeout(function(e) { hero_animation.addPath(e) }, 1000, 1);
        }
        else if (this.stage == 2)
        {
            // start coyote stage.
            this.maxParallel = 1;
            window.setTimeout(function(e) { hero_animation.addPath(e) }, 1000, this.stage);
        }

        if (this.stageTimes.length > this.stage) {
            window.setTimeout(function() { hero_animation.nextStage() }, this.stageTimes[this.stage]);
        }
    }

    addStar(svg, x, y, size, color){
        var dot = document.createElementNS("http://www.w3.org/2000/svg", "ellipse");
        dot.setAttribute("rx", size);
        dot.setAttribute("ry", size);
        dot.setAttribute("cx", x);
        dot.setAttribute("cy", y);
        dot.setAttribute("fill", color);
        svg.appendChild(dot);
        return dot;
    }

    addLayer(svg, index, num, size, color)
    {
        var margin = size * 4;
        var dims = this.getDimensions(svg);
        var maxHeight = dims.height - 2 * margin;
        var maxWidth =  dims.width - 2 * margin;
        var x = margin + (maxWidth / this.NUMLAYERS) * index;
        var layer = []
        for (var j = 0; j < num; j++)
        {
            var y = margin + (maxHeight / (num + 1)) * (j + 1);
            var dot = this.addStar(svg, x, y, size, color);
            layer.push(dot);
        }
        this.layers[index] = layer;
    }

    createPath(svg, thickness, color) {
        var points = "";
        var px = 0;
        var py = 0;
        var i = 0;
        var taken = {};
        var dots = []

        while (true)
        {
            var layer = this.layers[i];
            // pick something from this layer that hasn't already been picked,
            // if possible.
            while (true) {
                var j = parseInt(Math.random() * layer.length);
                if (taken[i] == undefined)
                {
                    taken[i] = {}
                }
                var map = taken[i]
                if (!map[j] || map.length == this.layerMap[i])
                {
                    map[j] = j;
                    break;
                }
            }

            var dot = layer[j];
            dots.push(dot);
            var x = dot.cx.baseVal.value;
            var y = dot.cy.baseVal.value;
            if (i == 0) {
                points += "m ";
            } else if (i == 1) {
                points += "l ";
            }
            points += (x - px) + "," + (y - py) + " ";
            px = x;
            py = y;

            // pick next layer.
            if (i == 0) {
                i++;
            }
            else if (i == this.NUMLAYERS - 1)
            {
                break;
            }
            else
            {
                var direction = 1;
                if (i > 1 && Math.random() < 0.25) {
                    // 25% chance we go backwards
                    direction = -1;
                }
                var maxsteps = (i > 4) ? 3 : 1;
                i += (maxsteps * direction);
                if (i > this.NUMLAYERS - 1)
                {
                    i = this.NUMLAYERS - 1;
                }
            }
        }

        var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("filter", "url(#glow-filter)");
        path.setAttribute("d", points);
        path.style.strokeWidth = thickness;
        path.style.stroke = color;
        path.style.fill = "none";
        svg.appendChild(path);

        // wrap path info including dots connecting the path.
        var info = new PathInfo();
        info.path = path;
        info.length = path.getTotalLength();
        info.offset = 0;
        info.opacity = 1;
        info.stage = this.stage;
        path.style.strokeDasharray  = [0, info.length];
        info.step = info.length / 100; // * 10ms timeout = 1 second
        info.dots = dots;
        return info;
    }

    fadePath(info) {
        info.opacity -= 0.01;
        if (info.opacity <= 0 || this.stage > 1){
            this.svg.removeChild(info.path);
            this.totalPaths--;
        } else {
            info.path.style.strokeOpacity = info.opacity;
            window.setTimeout(function(e) { hero_animation.fadePath(e) }, 10, info);
        }
    }

    animateBug(dot) {
        var rx = dot.rx.baseVal.value;
        if (rx < 30) {
            dot.setAttribute("rx", rx+0.1);
            dot.setAttribute("ry", rx+0.1);
            window.setTimeout(function(e) { hero_animation.animateBug(e) }, 30, dot);
        }
    }

    animatePath(info) {
        info.offset += info.step;
        info.path.style.strokeDasharray = [info.offset, info.length].join(' ');
        if (info.offset >= info.length) {
            if (this.stage == 3) {
                info.path.style.stroke = this.bugColor;
                var dot = info.dots[info.dots.length-1];
                dot.style.fill = this.bugColor;
                dot.setAttribute("filter", "url(#glow-filter-2)");
                window.setTimeout(function(e) { hero_animation.animateBug(e); }, 10, dot);
                info.offset = 0;
                this.totalReplays++;
                if (this.totalReplays < 3) {
                    window.setTimeout(function(e) { hero_animation.animatePath(e); }, 1000, info);
                }
                return;
            }
            info.path.style.stroke = this.activeColor;
            window.setTimeout(function(e) { hero_animation.fadePath(e); }, 10, info);
            if (info.stage == this.stage){
                window.setTimeout(function(e) { hero_animation.addPath(e); }, 10, info.stage);
            }
        } else {
            window.setTimeout(function(e) { hero_animation.animatePath(e); }, 10, info);
        }
    }

    addPath(s) {
        if (this.stage != s) {
            return;
        }
        var info = this.createPath(this.svg, 5, this.testColor);

        window.setTimeout(function(e) { hero_animation.animatePath(e); }, 10, info);

        // in crazy mode, pile up some more paths up to maxParallel in parallel.
        this.totalPaths++;
        if (this.totalPaths < this.maxParallel) {
            window.setTimeout(function(e) { hero_animation.addPath(e); }, 500, s);
        }
    }

};

var hero_animation = new HeroAnimation();
