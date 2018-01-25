torrontoControllers.controller('DashboardRecommendedCtrl', [
    '$scope', '$rootScope', '$routeParams', 'Movie',
    function ($scope, $rootScope, $routeParams, Movie) {
        $scope.title = "Рекомендации";
        $scope.fullArgs = 'systemlist=true&order=rkp';

        function buildParams() {
            return {
                systemlist: true,
                pagesize: 10,
                nocount: true,
                order: 'rkp'
            };
        }

        var params = buildParams();

        Movie.paginate(params, function (data) {
            $scope.movies = data.Movies;
        });
    }
]);