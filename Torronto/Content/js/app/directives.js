angular.module('torrontoDirectives', [])
    .directive('torrentquality', function () {
        return {
            restrict: 'E',
            scope: {
                video: '=video',
                audio: '=audio',
                translation: '=translation'
            },
            templateUrl: 'tpl-torrent-quality',
            controller: function ($scope) {
                //empty for a while
            }
        }
    })
    .directive('torrentdownload', function () {
        return {
            restrict: 'E',
            scope: {
                id: '=id',
                hash: '=hash',
            },
            templateUrl: 'tpl-torrent-download',
            controller: function ($scope) {
                //empty for a while
            }
        }
    })
    .directive('torrontoselect', function () {
        return {
            restrict: 'E',
            scope: {
                value: '=value',
                choices: '=choices'
            },
            templateUrl: 'tpl-select',
            controller: function ($scope) {
                $scope.title = 'Click me';
                $scope.isOpen = false;

                $scope.setValue = function (value) {
                    $scope.value = value;

                    for (var i = 0; i < $scope.choices.length; i++) {
                        var c = $scope.choices[i];

                        if (c.value == value) {
                            $scope.title = c.title;
                            break;
                        }
                    }

                    $scope.isOpen = false;
                };

                $scope.$watch('value', function (value) {
                    $scope.setValue(value);
                });
            }
        }
    })
    .directive('movieactions', function () {
        return {
            restrict: 'E',
            scope: {
                movie: '=movie',
            },
            templateUrl: 'tpl-movie-actions',
            controller: function ($scope, $rootScope, Movie) {
                $scope.toggleWatchStatus = function (movieItem) {
                    if (!checkLogin()) return;

                    var oldWatched = movieItem.IsWatched;
                    var method = oldWatched ? 'unwatch' : 'watch';
                    var old = saveAndReset(movieItem);

                    movieItem.IsWatched = !oldWatched;

                    Movie[method]({ movieID: movieItem.Self.ID }, {}, function () { }, function () {
                        restore(movieItem, old);
                    });
                }

                $scope.toggleWaitList = function (movieItem) {
                    if (!checkLogin()) return;

                    var oldInWaitList = movieItem.InWaitList;
                    var method = oldInWaitList ? 'unwait' : 'wait';
                    var old = saveAndReset(movieItem, true);

                    movieItem.InWaitList = !oldInWaitList;

                    Movie[method]({ movieID: movieItem.Self.ID }, {}, function () { }, function () {
                        restore(movieItem, old);
                    });
                }

                $scope.toggleDontWantStatus = function (movieItem) {
                    if (!checkLogin()) return;

                    var oldDontWant = movieItem.IsDontWant;
                    var method = oldDontWant ? 'undontwant' : 'dontwant';
                    var old = saveAndReset(movieItem);

                    movieItem.IsDontWant = !oldDontWant;

                    Movie[method]({ movieID: movieItem.Self.ID }, {}, function () { }, function () {
                        restore(movieItem, old);
                    });
                }

                $scope.setMark = function (movieItem, mark) {
                    if (!checkLogin()) return;

                    var old = saveAndReset(movieItem);

                    movieItem.Mark = mark;
                    movieItem.IsWatched = true;

                    Movie.mark({ movieID: movieItem.Self.ID, mark: mark }, {}, function () { }, function () {
                        restore(movieItem, old);
                    });
                }

                var saveAndReset = function (movieItem, skipReset) {
                    var result = {
                        Mark: movieItem.Mark,
                        IsWatched: movieItem.IsWatched,
                        InWaitList: movieItem.InWaitList,
                        IsDontWant: movieItem.IsDontWant
                    };

                    if (!skipReset) {
                        movieItem.Mark = null;
                        movieItem.IsWatched = false;
                        movieItem.InWaitList = false;
                        movieItem.IsDontWant = false;
                    }

                    return result;
                };

                var restore = function (movieItem, old) {
                    movieItem.Mark = old.Mark;
                    movieItem.IsWatched = old.IsWatched;
                    movieItem.InWaitList = old.InWaitList;
                    movieItem.IsDontWant = old.IsDontWant;
                };

                var checkLogin = function () {
                    if (!LoginInfo.IsLogged) {
                        $rootScope.$broadcast('alert.add', {
                            type: 'warning',
                            msg: 'Войди в систему, чтобы использовать эту фичу.'
                        });

                        return false;
                    }

                    return true;
                };
            }
        }
    })
    .directive('torrentactions', function () {
        return {
            restrict: 'E',
            scope: {
                torrent: '=torrent',
            },
            templateUrl: 'tpl-torrent-actions',
            controller: function ($scope, $rootScope, Torrent) {
                $scope.toggleSubscribeStatus = function (torrentItem) {
                    if (!checkLogin()) return;

                    var oldSubscribed = torrentItem.IsSubscribed;
                    var method = oldSubscribed ? 'unsubscribe' : 'subscribe';
                    var old = saveAndReset(torrentItem, true);

                    torrentItem.IsSubscribed = !oldSubscribed;

                    Torrent[method]({ torrentID: torrentItem.Self.ID }, {}, function () { }, function () {
                        restore(torrentItem, old);
                    });
                };

                $scope.toggleRssStatus = function (torrentItem) {
                    if (!checkLogin()) return;

                    var oldRss = torrentItem.IsRss;
                    var method = oldRss ? 'unrss' : 'rss';
                    var old = saveAndReset(torrentItem, true);

                    torrentItem.IsRss = !oldRss;

                    Torrent[method]({ torrentID: torrentItem.Self.ID }, {}, function () { }, function () {
                        restore(torrentItem, old);
                    });
                };

                var saveAndReset = function (torrentItem, skipReset) {
                    var result = {
                        IsSubscribed: torrentItem.IsSubscribed,
                        IsRss: torrentItem.IsRss
                    };

                    if (!skipReset) {
                        torrentItem.IsSubscribed = false;
                        torrentItem.IsRss = false;
                    }

                    return result;
                };

                var restore = function (torrentItem, old) {
                    torrentItem.IsSubscribed = old.IsSubscribed;
                    torrentItem.IsRss = old.IsRss;
                };

                var checkLogin = function () {
                    if (!LoginInfo.IsLogged) {
                        $rootScope.$broadcast('alert.add', {
                            type: 'warning',
                            msg: 'Войди в систему, чтобы использовать эту фичу.'
                        });

                        return false;
                    }

                    return true;
                };
            }
        }
    })
;