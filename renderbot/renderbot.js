var server = require('webserver').create();
var port = 19003;

var getPage = function(url, callback, error) {
    var page = require('webpage').create();

    page.open(url, function() {
    	setTimeout(function() {
            page.evaluate(function() {
                $('meta[name=fragment], script').remove()
            });

            callback(page.content);
            page.close();
    	}, 200);
    });
};

server.listen(port, function(request, response) {
    response.headers = {
        'Content-Type': 'text/html'
    };

    var regexp = /_escaped_fragment_=(.*)$/;
    var fragment = request.url.match(regexp);

    if (fragment == null) {
        response.statusCode = 404;
        response.write('Not found');
        response.close();
        return;
    }

    var url = 'http://localhost:19002/#!' + decodeURIComponent(fragment[1]);

    console.log(url);

    getPage(url, function(content) {
        response.statusCode = 200;
        response.write(content);
        response.close();
    })
});


