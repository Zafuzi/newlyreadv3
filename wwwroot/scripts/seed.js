$(function() {
    // serviceWorker();
    setRules();
    loadView("Index");
    checkURL(); //check if the URL has a reference to a page and load it
    setInterval("checkURL()", 250); //check for a change in the URL every 250 ms to detect if the history buttons have been used
});

var lasturl = ""; //here we store the current URL hash

function checkURL(hash) {
    if (!hash) hash = window.location.hash; //if no parameter is provided, use the hash value from the current address
    if (hash != lasturl) // if the hash value has changed
    {
        lasturl = hash; //update the current hash
        loadView(hash); // and load the new page
    }
}

function serviceWorker() {
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
}

function setRules() {
    $(".nav_loader").on("click", function(e) {
        e.preventDefault();
        let page = $(this).attr("href").replace("#", "");
        loadView(page);
        setRules();

    })
    $('.page_loader').on("click", function(e) {
        e.preventDefault();
        let category = $(this).attr("href").replace("#", "");
        loadView("Category/" + category);
        setRules();
    });
    $('.article_loader').on("click", function(e) {
        e.preventDefault();
        let url = $(this).attr("data-url");
        let title = $(this).attr("data-title");
        url = encodeURIComponent(url);
        console.log("ARTICLE: " + url + " " + title);
        loadArticle("extract/" + url + title);
    });
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

    $("img").on("error", function() {
        imgError(this);
    })
}

function getSources() {
    var headers = new Headers();
    headers.append('Content-Type', 'application/json');
    var request = new Request("/api/v1/sources", headers);

    fetch(request)
        .then(response => {
            if (response.status !== 200) {
                console.log('Looks like there was a problem. Status Code: ' + response.status);
                return;
            }
            response.json().then(json => {
                console.log(json);
            })
        })
}

function callAPI(endpoint) {
    var headers = new Headers();
    headers.append('Content-Type', 'application/json');
    var request = new Request("/api/v1/" + endpoint, headers);

    return fetch(request)
        .then(response => {
            if (response.status !== 200) {
                console.log('Looks like there was a problem. Status Code: ' + response.status);
                return;
            }
            return response.json();
        })
}

function loadArticle(endpoint) {
    var headers = new Headers();
    headers.append('Content-Type', 'application/json');
    var request = new Request("/api/v1/" + endpoint, headers);

    fetch(request)
        .then(response => {
            if (response.status !== 200) {
                console.log('Looks like there was a problem. Status Code: ' + response.status);
                return;
            }
            response.text().then(text => {
                $("#content").html(text);
                setRules();
            })
        })
}

function loadView(viewName) {
    var headers = new Headers();
    headers.append('Content-Type', 'text/html');
    var request = new Request("/api/v1/views/" + viewName, headers);

    fetch(request)
        .then(response => {
            if (response.status !== 200) {
                console.log('Looks like there was a problem. Status Code: ' + response.status);
                return;
            }
            response.text().then(text => {
                $("#content").html(text);
                lasturl = viewName;
                setRules();
            })
        })
}

function imgError(image) {
    image.onerror = "";
    image.src = "../images/news.png";
    return true;
}

FeaturedArticles = [];
// Classes
class FeaturedArticle {
    constructor(url, title, content) {
        this.url = url;
        this.title = title;
        this.content = content;

        this.baseElement = $("<a href='#' class='article article_loader' data-url='" + this.url + "' data-title='" + this.title + "'>");

        this.containerElement = $("<div>");
        this.imageContainerElement = $("<div class='card-image'>");
        this.contentContainerElement = $("<div class='card-content'>");

        this.addTitle();
        this.addImage();

        this.containerElement.append(this.imageContainerElement, this.contentContainerElement);
        this.baseElement.append(this.containerElement);
    }
    addTitle() {
        let self = this;
        let title = $("<h4>").text(self.title);
        self.contentContainerElement.append(title);
    }
    addImage() {
        let self = this;
        let img = $("<img class='lazyload' onerror='imgError(this);' src='/images/news.png' alt='Sorry, this article image could not be loaded'/>");
        if (self.content.images.length > 0) {
            img.attr("src", self.content.images[0].url);
        }
        self.imageContainerElement.append(img);
    }
    add(parent) {
        let self = this;
        self.parent = parent;
        $(parent).append(self.baseElement);
    }
    hide() {
        let self = this;
        self.toggle();
    }
    destroy() {
        let self = this;
        self.parent.remove(self);
    }

}

CategoryArticles = [];
class CategoryArticle {
    constructor(url, title, content) {
        this.url = url;
        this.title = title;
        this.content = content;

        this.baseElement = $("<a class='article'>");
        this.baseElement.attr("href", this.url);

        this.containerElement = $("<div>");
        this.imageContainerElement = $("<div class='card-image'>");
        this.contentContainerElement = $("<div class='card-content'>");

        // this.addTitle();
        // this.addImage();

        this.containerElement.append(this.imageContainerElement, this.contentContainerElement);
        this.baseElement.append(this.containerElement);
    }
    addTitle() {
        let self = this;
        let title = $("<h4>").text(self.title);
        self.contentContainerElement.append(title);
    }
    addImage() {
        let self = this;
        let img = $("<img class='lazyload' onerror='imgError(this);' src='/images/news.png' alt='Sorry, this article image could not be loaded'/>");
        if (self.content.images.length > 0) {
            img.attr("src", self.content.images[0].url);
        }
        self.imageContainerElement.append(img);
    }
    add(parent) {
        let self = this;
        self.parent = parent;
        $(parent).append(self.baseElement);
    }
    hide() {
        let self = this;
        self.toggle();
    }
    destroy() {
        let self = this;
        self.parent.remove(self);
    }

}