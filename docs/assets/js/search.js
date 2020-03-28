// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

function fetchJson(url, handler)
{
    const xhr = new XMLHttpRequest();
    // listen for `onload` event
    xhr.onload = () => {
        // process response
        if (xhr.status == 200) {
            handler(xhr.responseText);
        } else {
            console.error('Error downloading:  ' + url + ", error=" + xhr.status);
        }
    };

    // create a `GET` request
    xhr.open('GET', url);

    // send request
    xhr.send();
}

function setupSearchJson(jsonText)
{
    var jsonData = JSON.parse(jsonText);
    var len = jsonData.length
    for (var i = 0; i < len; i++) {
        var title = jsonData[i]["title"]
        // split CamelCase and "." separated names into separate words for search tags.
        var words = title.split(".")
        var tags = []
        var numwords = words.length
        for (var j = 0; j < numwords; j++) {
            var word = words[j];
            tags.push(word);
            var separated = word.replace(/([A-Z])/g, " $1").split(' ');
            var m = separated.length
            for (var k = 0; k < m; k++) {
                var s = separated[k];
                if (s.trim() != "") {
                 tags.push(s)
                }
            }
        }
        tags = tags.join(', ');
        jsonData[i]["tags"] = tags;
    }

    searchBox = document.getElementById('search-input');

    var lastTime = 0;

    searchBox.addEventListener("keydown", function(){
        var d = new Date();
        // if it has been more than 10 seconds scroll search wrapper into view.
        if (d.getTime() - lastTime > 10000) {
            lastTime = d.getTime();
            var wrapper = $("#results-container");
            if (wrapper.length > 0){
                wrapper[0].scrollIntoView();
            }
        }
    }, true);

    SimpleJekyllSearch({
        searchInput: searchBox,
        resultsContainer: document.getElementById('results-container'),
        json: jsonData,
        searchResultTemplate: '<div class="search-title"><a href="{url}"><h3> {title}</h3></a></div><p>{excerpt}</p></div><hr> ',
        noResultsText: 'No results found',
        limit: 10,
        fuzzy: false,
        exclude: []
    })
}