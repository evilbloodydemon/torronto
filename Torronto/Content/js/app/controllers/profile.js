torrontoControllers.controller('ProfileCtrl', [
    '$scope', '$rootScope', '$routeParams', '$location', '$route', 'User',
    function ($scope, $rootScope, $routeParams, $location, $route, User) {
        $scope.tab = $routeParams.tab || 'filter';
        $scope.loginsActive = $scope.tab == 'logins';

        $scope.videoQualities = Resources.videoQualities;
        $scope.audioQualities = Resources.audioQualities;
        $scope.translationQualities = Resources.translationQualities;

        $scope.sizes = {};

        var user = User.profile({}, function (data) {
            $scope.user = data;

            angular.forEach(data.FilterSizes.split(","), function (v, k) {
                $scope.sizes[v] = true;
            });
        });

        function buildSizes() {
            var result = [];

            for (var i = 0; i < 5; i++) {
                if ($scope.sizes[i]) result.push(i);
            }

            return result.join(',');
        };

        $scope.saveProfile = function () {
            user.FilterSizes = buildSizes();
            user.$save(function (u, putResponseHeaders) {
                $rootScope.$broadcast('alert.add', {
                    type: 'success',
                    msg: 'Изменения сохранены'
                });
            });

            //todo add success/fail handling
            //todo move to service
           LoginInfo.FilterVideo = user.FilterVideo;
           LoginInfo.FilterAudio = user.FilterAudio;
           LoginInfo.FilterTraslation = user.FilterTraslation;
           LoginInfo.FilterSizes = user.FilterSizes;
        };

        $scope.mergeAccounts = function() {
            User.merge({}, {}, function() {
                $route.reload();
            });
        };

        $scope.stopMerge = function() {
            User.stopmerge({}, {}, function () {
                $route.reload();
            });
        }
    }
]);