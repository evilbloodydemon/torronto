var torrontoControllers = angular.module('torrontoControllers', ['ngResource']);

function encodeQueryData(data) {
    var ret = [];

    for (var d in data) {
        if (!!data[d]) {
            ret.push(encodeURIComponent(d) + "=" + encodeURIComponent(data[d]));
        }
    }

    return ret.join("&");
}

var Resources = {
    videoQualities: [
        { title: 'Любое', value: 0 },
        { title: 'Высокое', value: 8 },
        { title: 'Хорошее+', value: 12 },
        { title: 'Хорошее', value: 4 },
        { title: 'Посредственное', value: 2 },
        { title: 'Плохое', value: 1 }
    ],
    audioQualities: [
        { title: 'Любое', value: 0 },
        { title: 'Высокое', value: 8 },
        { title: 'Хорошее+', value: 12 },
        { title: 'Хорошее', value: 4 },
        { title: 'Плохое', value: 1 }
    ],
    translationQualities: [
        { title: 'Любой', value: 0 },
        { title: 'Любая озвучка', value: 12 },
        { title: 'Дубляж', value: 8 },
        { title: 'Закадровый', value: 4 },
        { title: 'Субтитры', value: 1 }
    ],
    movieStatuses: [
        { title: 'Все', value: 0 },
        { title: 'Недавние премьеры', value: 2 },
        { title: 'Скоро выходят', value: 1 }
    ],
    movieOrders: [
        { title: 'Название', value: 'title' },
        { title: 'Рейтинг кинопоиска', value: 'rkp' },
        { title: 'Рейтинг IMDB', value: 'rimdb' },
        { title: 'Моя оценка', value: 'ruser' },
        { title: 'Доступное качество', value: 'quality' },
        { title: 'Дата появления на сайте', value: 'added' },
        { title: 'Недельная популярность', value: 'topweek' }
    ],
};

torrontoControllers.controller('TorrentListCtrl', [
    '$scope', '$routeParams', '$location', 'Torrent',
    function ($scope, $routeParams, $location, Torrent) {
        $scope.searchTerm = $routeParams.search || '';
        $scope.waitList = ($routeParams.waitlist === 'true');
        $scope.subscription = ($routeParams.subscription === 'true');
        $scope.videoQuality = $routeParams.vq || 0;
        $scope.audioQuality = $routeParams.aq || 0;
        $scope.translationQuality = $routeParams.tq || 0;

        $scope.sizes = {};
        $scope.pgTotalItems = 0;
        $scope.pgCurrent = 1;
        $scope.pgSize = 0;

        $scope.isLogged = LoginInfo.IsLogged;

        $scope.videoQualities = Resources.videoQualities;
        $scope.audioQualities = Resources.audioQualities;
        $scope.translationQualities = Resources.translationQualities;

        var sizeParam = $routeParams.sizes || "";
        angular.forEach(sizeParam.split(","), function (v, k) {
            $scope.sizes[v] = true;
        });

        $scope.$watchGroup(
            ['waitList', 'subscription', 'videoQuality', 'audioQuality', 'translationQuality', 'buildSizes()'],
            function (newValues, oldValues, scope) {
                if (!!$scope.torrents) {
                    $scope.searchClick();
                }
            }
        );

        $scope.buildSizes = function () {
            var result = [];

            for (var i = 0; i < 5; i++) {
                if ($scope.sizes[i]) result.push(i);
            }

            return result.join(',');
        };

        function buildParams() {
            return {
                page: page,
                search: $scope.searchTerm,
                waitlist: $scope.waitList,
                subscription: $scope.subscription,
                sizes: $scope.buildSizes(),
                vq: $scope.videoQuality,
                aq: $scope.audioQuality,
                tq: $scope.translationQuality
            };
        };

        var page = $routeParams.page || 1;
        var params = buildParams();

        Torrent.paginate(params, function (data) {
            $scope.torrents = data.Torrents;
            $scope.pgTotalItems = data.TotalItems;
            $scope.pgSize = data.PageSize;
            $scope.pgCurrent = page;
        });

        $scope.pageChanged = function () {
            params.page = $scope.pgCurrent;
            $location.url('/torrents?' + encodeQueryData(params));
        };

        $scope.searchClick = function () {
            var searchParams = buildParams();
            searchParams.page = 1;

            $location.url('/torrents?' + encodeQueryData(searchParams));
        };
    }
]);

torrontoControllers.controller('MovieListCtrl', [
    '$scope', '$rootScope', '$routeParams', '$location', '$http', '$timeout', 'Movie',
    function ($scope, $rootScope, $routeParams, $location, $http, $timeout, Movie) {
        $scope.searchTerm = $routeParams.search || '';
        $scope.waitList = ($routeParams.waitlist === 'true');
        $scope.systemList = ($routeParams.systemlist === 'true');
        $scope.movieStatus = $routeParams.ms || 0;
        $scope.order = $routeParams.order || 'rkp';
        $scope.actors = ($routeParams.actors || '').split(',');
        $scope.actorsList = null;
        $scope.kinopoisk_id = $routeParams.kinopoisk_id || 0;
        $scope.open_single = $routeParams.open_single || 0;

        $scope.pgTotalItems = 0;
        $scope.pgCurrent = 1;
        $scope.pgSize = 0;

        $scope.isLogged = LoginInfo.IsLogged;

        $scope.movieStatuses = Resources.movieStatuses;
        $scope.movieOrders = Resources.movieOrders;

        $scope.$watchGroup(
            ['waitList', 'systemList', 'order', 'movieStatus', 'actors.join(", ")'],
            function (newValues, oldValues, scope) {
                if (!!$scope.movies) {
                    $scope.searchClick();
                }
            }
        );

        function buildParams() {
            return {
                page: page,
                search: $scope.searchTerm,
                waitlist: $scope.waitList,
                systemlist: $scope.systemList,
                ms: $scope.movieStatus,
                order: $scope.order,
                actors: $scope.actors.join(','),
                kinopoiskId: $scope.kinopoisk_id
            };
        }

        var page = $routeParams.page || 1;
        var params = buildParams();

        Movie.paginate(params, function (data) {
            if ($scope.open_single && data.Movies.length === 1) {
                $location.url('/movies/' + data.Movies[0].Self.ID);
                return;
            }

            $scope.movies = data.Movies;
            $scope.actorsList = data.Actors;

            $scope.pgTotalItems = data.TotalItems;
            $scope.pgSize = data.PageSize;
            $scope.pgCurrent = page;
        });

        $scope.pageChanged = function () {
            params.page = $scope.pgCurrent;
            $location.url('/movies?' + encodeQueryData(params));
        };

        $scope.searchClick = function () {
            var searchParams = buildParams();
            searchParams.page = 1;

            $location.url('/movies?' + encodeQueryData(searchParams));
        };

        $scope.itemSelect = function () {
            $location.url('/movies/' + $scope.movieSelected.originalObject.ID);
        }

        $scope.removeActor = function (actor) {
            _.remove($scope.actors, function (x) { return x == actor.ID; });
            _.remove($scope.actorsList, function (x) { return x.ID == actor.ID; });
        }
    }
]);

torrontoControllers.controller('MovieDetailCtrl', [
    '$scope', '$rootScope', '$routeParams', '$timeout', 'Movie', 'Torrent',
    function ($scope, $rootScope, $routeParams, $timeout, Movie, Torrent) {
        $scope.movie = {};
        $scope.torrents = [];
        $scope.isMovieDetails = true;

        Movie.get({ movieID: $routeParams.id }, function (data) {
            $scope.movie = data;

            updateSocialShare(data.Self.Title);
        });

        var params = {
            movieID: $routeParams.id,
            pagesize: 100,
            nocount: true,
            order: 'size'
        };

        Torrent.paginate(params, function (data) {
            $scope.torrents = data.Torrents;
        });

        $scope.addToWaitList = function () {
            $timeout(function () {
                angular
                    .element('.movie-actions i:first')
                    .triggerHandler('click');
            }, 0);
        };
    }
]);

torrontoControllers.controller('TorrentDetailCtrl', [
    '$scope', '$routeParams', 'Torrent',
    function ($scope, $routeParams, Torrent) {
        $scope.torrent = {};

        Torrent.get({ torrentID: $routeParams.id }, function (data) {
            $scope.torrent = data;

            updateSocialShare(data.Self.Title);
        });
    }
]);

torrontoControllers.controller('AlertsCtrl', [
    '$scope',
    function ($scope) {
        $scope.alerts = [];

        $scope.$on('alert.add', function (evetn, data) {
            $scope.alerts.push(data);

            var len = $scope.alerts.length;
            var maxLen = 1;

            if (len > maxLen) {
                $scope.alerts.splice(0, len - maxLen);
            }
        });

        $scope.closeAlert = function (index) {
            $scope.alerts.splice(index, 1);
        };
    }
]);

torrontoControllers.controller('FrontPageCtrl', [
    '$scope', '$location',
    function ($scope, $location) {
        if (LoginInfo.IsLogged) {
            $location.url('/dashboard');
        }
    }
]);