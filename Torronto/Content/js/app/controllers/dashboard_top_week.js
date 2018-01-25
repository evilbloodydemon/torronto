torrontoControllers.controller('DashboardTopWeekCtrl', [
    '$scope', '$rootScope', '$routeParams', 'Movie',
    function ($scope, $rootScope, $routeParams, Movie) {
        $scope.title = "Самые популярные за неделю";
        $scope.fullArgs = 'order=topweek';

        function buildParams() {
            return {
                pagesize: 10,
                nocount: true,
                order: 'topweek'
            };
        }

        var params = buildParams();

        Movie.paginate(params, function (data) {
            $scope.movies = data.Movies;
        });
    }
]);