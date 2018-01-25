using LinqToDB;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Threading;
using Torronto.BLL.Ext;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.BLL
{
    public class QueueService
    {
        private static readonly bool _isSphinxEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["Search.IsSphinxEnabled"]);

        private static readonly BlockingCollection<int> _torrentMatchQueue = new BlockingCollection<int>();
        private static readonly BlockingCollection<int> _torrentDetailQueue = new BlockingCollection<int>();
        private static readonly BlockingCollection<int> _movieMatchQueue = new BlockingCollection<int>();
        private static readonly BlockingCollection<int> _movieDetailQueue = new BlockingCollection<int>();
        private static readonly BlockingCollection<int> _movieRatingQueue = new BlockingCollection<int>();
        private static readonly BlockingCollection<int> _movieRecommendationsQueue = new BlockingCollection<int>();
        private static readonly BlockingCollection<int> _reindexTorrentQueue = new BlockingCollection<int>();
        private static readonly BlockingCollection<int> _reindexMovieQueue = new BlockingCollection<int>();

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly SearchService _searchService;
        private readonly RutorService _rutorService;
        private readonly KinopoiskService _kinopoiskService;
        private readonly EmailService _emaiService;
        private readonly MovieService _movieService;
        private readonly TorrentService _torrentService;

        public QueueService(
            SearchService searchService,
            RutorService rutorService,
            KinopoiskService kinopoiskService,
            EmailService emaiService,
            MovieService movieService,
            TorrentService torrentService
        )
        {
            _searchService = searchService;
            _rutorService = rutorService;
            _kinopoiskService = kinopoiskService;
            _emaiService = emaiService;
            _movieService = movieService;
            _torrentService = torrentService;
        }

        protected static void ProcessQueueItems(BlockingCollection<int> queue, string name, Action<int> itemOperation, Func<int> delay)
        {
            while (!queue.IsCompleted)
            {
                int item;

                try
                {
                    item = queue.Take();
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                Thread.Sleep(delay());

                try
                {
                    itemOperation(item);
                }
                catch (Exception ex)
                {
                    _logger.Error("Exception in ProcessQueueItems " + name, ex);
                }
            }
        }

        public void Start()
        {
            new Thread(MatchTorrents).Start();
            new Thread(DetailTorrents).Start();
            new Thread(MatchMovies).Start();
            new Thread(DetailMovies).Start();
            new Thread(RateMovies).Start();
            new Thread(RecommendMovies).Start();
            new Thread(ReindexTorrents).Start();
            new Thread(ReindexMovies).Start();
        }

        public void Stop()
        {
            _torrentMatchQueue.CompleteAdding();
            _torrentDetailQueue.CompleteAdding();
            _movieMatchQueue.CompleteAdding();
            _movieDetailQueue.CompleteAdding();
            _movieRatingQueue.CompleteAdding();
            _movieRecommendationsQueue.CompleteAdding();
            _reindexMovieQueue.CompleteAdding();
            _reindexTorrentQueue.CompleteAdding();
        }

        //todo rewrite these separate functions as queue objects with handlers

        #region Add to queue

        public static void AddTorrentForMatch(int torrentId)
        {
            _torrentMatchQueue.Add(torrentId);
        }

        public static void AddTorrentForDetails(int torrentId)
        {
            _torrentDetailQueue.Add(torrentId);
        }

        public static void AddMovieForMatch(int movieId)
        {
            _movieMatchQueue.Add(movieId);
        }

        public static void AddMovieForDetails(int movieId)
        {
            _movieDetailQueue.Add(movieId);
        }

        public static void AddMovieForRating(int movieId)
        {
            _movieRatingQueue.Add(movieId);
        }

        public static void AddMovieForRecommendation(int movieId)
        {
            _movieRecommendationsQueue.Add(movieId);
        }

        public static void AddMovieForReindex(int movieId)
        {
            _reindexMovieQueue.Add(movieId);
        }

        public static void AddTorrentForReindex(int torrentId)
        {
            _reindexTorrentQueue.Add(torrentId);
        }

        #endregion Add to queue

        #region Init queue processing

        private void RateMovies()
        {
            ProcessQueueItems(_movieRatingQueue, "RateMovies", _kinopoiskService.UpdateMovieRating, () => 250);
        }

        private void RecommendMovies()
        {
            var random = new Random();

            ProcessQueueItems(_movieRecommendationsQueue, "RecommendMovies", _kinopoiskService.GetRecommendations, () => 5000 + random.Next(10000));
        }

        private void MatchTorrents()
        {
            ProcessQueueItems(_torrentMatchQueue, "MatchTorrents", MatchTorrent, () => 0);
        }

        private void DetailTorrents()
        {
            var random = new Random();

            ProcessQueueItems(_torrentDetailQueue, "DetailTorrents", DetailTorrent, () => 4000 + random.Next(2000));
        }

        private void DetailMovies()
        {
            var random = new Random();

            ProcessQueueItems(_movieDetailQueue, "DetailMovies", _kinopoiskService.ProcessDetailsSingle, () => 1000 + random.Next(1500));
        }

        private void MatchMovies()
        {
            ProcessQueueItems(_movieMatchQueue, "MatchMovies", MatchMovie, () => 0);
        }

        private void ReindexMovies()
        {
            ProcessQueueItems(_reindexMovieQueue, "ReindexMovies", ReindexMovie, () => 0);
        }

        private void ReindexTorrents()
        {
            ProcessQueueItems(_reindexTorrentQueue, "ReindexTorrents", ReindexTorrent, () => 0);
        }

        #endregion Init queue processing

        #region Process queue item

        private void DetailTorrent(int torrentId)
        {
            _rutorService.ProcessDetailsSingle(torrentId);

            var movies = _movieService.CreateMoviesFromTorrents();

            foreach (var movie in movies.Where(x => x.ID != null))
            {
                var movieId = movie.ID.GetValueOrDefault();
                AddMovieForDetails(movieId);
                AddMovieForRating(movieId);
            }
        }

        private void MatchMovie(int movieId)
        {
            using (var db = new DbTorronto())
            {
                _logger.Info("Matching movie #{0}", movieId);

                var movie = db.Movie.FirstOrDefault(t => t.ID == movieId);

                if (movie?.KinopoiskID != null)
                {
                    db.Torrent
                        .Where(t => t.MovieID == null && t.KinopoiskID == movie.KinopoiskID)
                        .Set(f => f.MovieID, movieId)
                        .Update();

                    UpdateBestVideoQuality(db, movie);
                }
            }
        }

        private void MatchTorrent(int torrentId)
        {
            using (var db = new DbTorronto())
            {
                _logger.Info("Matching torrent #{0}", torrentId);

                var torrent = db.Torrent.FirstOrDefault(t => t.ID == torrentId);

                if (torrent != null)
                {
                    var predicate = PredicateBuilder.False<Movie>();

                    if (torrent.KinopoiskID != null) predicate = predicate.Or(m => m.KinopoiskID == torrent.KinopoiskID);
                    if (torrent.ImdbID != null) predicate = predicate.Or(m => m.ImdbID == torrent.ImdbID);

                    var movies = db.Movie.Where(predicate).ToList();

                    if (movies.Count > 1)
                    {
                        _logger.Warn("torrent #{0} matches to multiple movies", torrentId);
                        return;
                    }

                    var movie = movies.FirstOrDefault();

                    if (movie != null)
                    {
                        db.Torrent
                            .Where(x => x.ID == torrent.ID)
                            .Set(f => f.MovieID, movie.ID)
                            .Update();

                        UpdateBestVideoQuality(db, movie);

                        SendNotifications(db, movie, torrent);
                    }
                }
            }
        }

        public static void UpdateBestVideoQuality(DbTorronto db, Movie movie)
        {
            var videoQuality = db.Torrent
                .Where(t => t.MovieID == movie.ID)
                .Select(t => (VideoQuality?)t.VideoQuality)
                .Max() ?? VideoQuality.Unknown;

            db.Movie
                .Where(m => m.ID == movie.ID)
                .Set(f => f.BestVideoQuality, videoQuality)
                .Update();
        }

        private void SendNotifications(DbTorronto db, Movie movie, Torrent torrent)
        {
            var users = db.User
                .Join(db.MovieUser, u => u.ID, mu => mu.UserID, (u, mu) => new { u, mu })
                .Where(t => t.mu.MovieID == movie.ID
                    && t.mu.IsWaitlist
                    && !t.mu.IsWatched)
                .Select(t => t.u)
                .ToList();

            var torrentId = torrent.ID.GetValueOrDefault();
            var filteredUsers = users
                .Where(user => !string.IsNullOrEmpty(user.Email))
                .Where(user => IsMatchUserFilter(torrent, user))
                .Where(user => !db.TorrentUser.Any(tu =>
                    tu.TorrentID == torrentId
                    && tu.UserID == user.ID
                    && tu.EmailSent != null))
                .ToList();

            foreach (var user in filteredUsers)
            {
                _emaiService.NotifyUserAboutMovie(user, torrent, movie);

                db.TorrentUser.InsertOrUpdate(
                    () => new TorrentUser
                    {
                        UserID = user.ID.GetValueOrDefault(),
                        TorrentID = torrentId,
                        EmailSent = DateTime.UtcNow,
                        IsSubscribed = false,
                    },
                    old => new TorrentUser
                    {
                        EmailSent = DateTime.UtcNow
                    }
                );
            }
        }

        public static bool IsMatchUserFilter(Torrent torrent, User user)
        {
            var result = true;

            if (user.FilterVideo > VideoQuality.Unknown) result = (result && (torrent.VideoQuality & user.FilterVideo) > 0);
            if (user.FilterAudio > AudioQuality.Unknown) result = (result && (torrent.AudioQuality & user.FilterAudio) > 0);
            if (user.FilterTraslation > Translation.Unknown) result = (result && (torrent.Translation & user.FilterTraslation) > 0);

            var filterSizes = (user.FilterSizes ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    int r;
                    int.TryParse(s.Trim(), out r);
                    return r;
                })
                .ToArray();

            if (filterSizes.Length > 0)
            {
                var index = TorrentService.TorrentSizes
                    .TakeWhile(tsize => torrent.Size < tsize.Item1 || torrent.Size > tsize.Item2)
                    .Count();

                result = (result && filterSizes.Contains(index));
            }

            return result;
        }

        private void ReindexMovie(int movieId)
        {
            if (!_isSphinxEnabled) return;

            var movie = _movieService.GetById(movieId);

            if (movie != null)
            {
                _searchService.IndexMovie(movie);
            }
        }

        private void ReindexTorrent(int torrentId)
        {
            if (!_isSphinxEnabled) return;

            var torrent = _torrentService.GetById(torrentId);

            if (torrent != null)
            {
                _searchService.IndexTorrent(torrent);
            }
        }

        #endregion Process queue item
    }
}