torrontoControllers.controller('DashboardWaitlistCtrl', [
    '$scope', '$rootScope', '$routeParams', 'Movie',
    function ($scope, $rootScope, $routeParams, Movie) {
        $scope.title = "Список ожидания";
        $scope.highlight = true;
        $scope.fullArgs = 'waitlist=true&order=quality';

        function buildParams() {
            return {
                waitlist: true,
                pagesize: 10,
                nocount: true,
                order: 'quality'
            };
        }

        var params = buildParams();

        Movie.paginate(params, function (data) {
            $scope.movies = data.Movies;
        });
    }
]);