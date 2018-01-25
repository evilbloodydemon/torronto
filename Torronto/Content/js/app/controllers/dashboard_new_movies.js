torrontoControllers.controller('DashboardNewMoviesCtrl', [
    '$scope', '$rootScope', '$routeParams', 'Movie',
    function ($scope, $rootScope, $routeParams, Movie) {
        $scope.title = "Новые фильмы на сайте";
        $scope.fullArgs = 'order=added';

        function buildParams() {
            return {
                pagesize: 10,
                nocount: true,
                order: 'added'
            };
        }

        var params = buildParams();

        Movie.paginate(params, function (data) {
            $scope.movies = data.Movies;
        });
    }
]);