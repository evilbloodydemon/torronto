torrontoControllers.controller('DashboardTorrentsCtrl', [
    '$scope', '$rootScope', '$routeParams', 'Torrent',
    function ($scope, $rootScope, $routeParams, Torrent) {
        $scope.profile = LoginInfo;
        $scope.noListIcons = true;

        function buildParams() {
            return {
                subscription: true
            };
        }

        var params = buildParams();
        params.pagesize = 30;
        params.nocount = true;

        Torrent.paginate(params, function (data) {
            $scope.torrents = data.Torrents;
        });

        $scope.allParams = encodeQueryData(buildParams());
    }
]);