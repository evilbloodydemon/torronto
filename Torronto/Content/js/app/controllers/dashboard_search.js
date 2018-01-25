torrontoControllers.controller('DashboardSearchCtrl', [
    '$scope', '$location',
    function ($scope, $location) {
        $scope.searchTerm = '';

        $scope.searchClick = function () {
            $location.url('/movies?' + encodeQueryData({search: $scope.searchTerm}));
        };

        $scope.itemSelect = function () {
            $location.url('/movies/' + $scope.movieSelected.originalObject.ID);
        }
    }
]);