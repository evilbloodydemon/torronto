angular.module('torrontoFilters', [])
    .filter('nullable', function () {
        return function (input) {
            return input ? input : '\u2718';
        };
    })
    .filter('clrdate', function() {
        return function (input) {
            return input ? /Date\((-?\d+)/.exec(input)[1] : '';
        };
    })
    .filter('mbsize', function() {
        return function (input) {
            return input > 1024 ? (Math.round(input / 1024 * 1000) / 1000) + ' Gb'  : Math.round(input) + ' Mb';
        };
    })
    ;
