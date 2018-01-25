var torrontoApp = angular.module('torrontoApp', [
    'ngRoute',
    'ngSanitize',
    'torrontoFilters',
    'torrontoControllers',
    'torrontoServices',
    'torrontoDirectives',
    'ui.bootstrap.pagination',
    'ui.bootstrap.buttons',
    'ui.bootstrap.alert',
    'ui.bootstrap.dropdown',
    'ui.bootstrap.tabs',
    'ui.bootstrap.tpls',
    'angulartics',
    'angulartics.google.analytics',
    'angucomplete'
]);

torrontoApp.config(function ($routeProvider, $locationProvider) {
    $locationProvider
        .hashPrefix('!');

    $routeProvider
        .when('/', {
            controller: 'FrontPageCtrl',
            templateUrl: 'Content/tmpl/frontpage.html'
        })
        .when('/dashboard', {
            controller: 'DashboardCtrl',
            templateUrl: 'Content/tmpl/dashboard.html'
        })
        .when('/torrents', {
            controller: 'TorrentListCtrl',
            templateUrl: 'Content/tmpl/torrent-list.html'
        })
        .when('/movies', {
            controller: 'MovieListCtrl',
            templateUrl: 'Content/tmpl/movie-list.html'
        })
        .when('/movies/:id', {
            controller: 'MovieDetailCtrl',
            templateUrl: 'Content/tmpl/movie-details.html'
        })
        .when('/torrents/:id', {
            controller: 'TorrentDetailCtrl',
            templateUrl: 'Content/tmpl/torrent-details.html'
        })
        .when('/profile', {
            controller: 'ProfileCtrl',
            templateUrl: 'Content/tmpl/profile.html'
        })
        .otherwise({ redirectTo: '/' });

})
.config([
    '$compileProvider',
    function ($compileProvider) {
        $compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|ftp|mailto|magnet):/);
    }
])
.run(function ($rootScope, $location) {
      $rootScope.$on("$routeChangeStart", function (event, next, current) {
          updateSocialShare();
      });
  });