var torrontoServices = angular.module('torrontoServices', ['ngResource']);

torrontoServices.factory('Torrent', [
    '$resource', function ($resource) {
        return $resource('/api/torrents/:torrentID', { torrentID: '@ID' }, {
            paginate: { method: 'GET', params: {} },
            subscribe: { method: 'PUT', params: { subscribe: true } },
            unsubscribe: { method: 'DELETE', params: { subscribe: true } },
            rss: { method: 'PUT', params: { rss: true } },
            unrss: { method: 'DELETE', params: { rss: true } },
        });
    }
]);

torrontoServices.factory('Movie', [
    '$resource', function ($resource) {
        return $resource('/api/movies/:movieID', { movieID: '@ID' }, {
            waitlist: { method: 'GET', params: { waitlist: true }, isArray: true },
            wait: { method: 'PUT', params: { waitlist: true } },
            unwait: { method: 'DELETE', params: { waitlist: true } },
            watch: { method: 'PUT', params: { watched: true } },
            unwatch: { method: 'DELETE', params: { watched: true } },
            dontwant: { method: 'PUT', params: { dontwant: true } },
            undontwant: { method: 'DELETE', params: { dontwant: true } },
            mark: { method: 'PUT', params: {} },
            paginate: { method: 'GET', params: {} }
        });
    }
]);

torrontoServices.factory('User', [
    '$resource', function ($resource) {
        return $resource('/api/users/:userID', { userID: '@ID' }, {
            profile: { method: 'GET', params: { profile: true } },
            merge: { method: 'PUT', params: { merge: true } },
            stopmerge: { method: 'PUT', params: { stopmerge: true } }
        });
    }
]);