using LinqToDB;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using LinqToDB.Linq;
using Torronto.BLL.Ext;
using Torronto.BLL.Models;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.BLL
{
    public class MovieService
    {
        private static readonly bool _isSphinxEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["Search.IsSphinxEnabled"]);
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly SearchService _searchService;
        private readonly int[] _marks = { 4, 7, 10 };

        public MovieService(SearchService searchService)
        {
            _searchService = searchService;
        }

        public MoviePagination GetMovies(int? userId, MovieSearchParams search, PaginationParams pageParams)
        {
            using (var db = new DbTorronto())
            {
                var filter = db.Movie
                    .NoCopyrighted()
                    .SelectMany(m => db.MovieUser.Where(x => x.MovieID == m.ID && x.UserID == userId).DefaultIfEmpty(),
                        (movie, movieUser) => new
                        {
                            M = movie,
                            MuWaitList = movieUser.IsWaitlist,
                            MuUserWatched = movieUser.IsWatched,
                            MuDontWant = movieUser.IsDontWant,
                            WlDate = movieUser.Created,
                            MuMark = movieUser.Mark,
                        });

                if (!string.IsNullOrEmpty(search.Search))
                {
                    if (_isSphinxEnabled)
                    {
                        var movieIds = _searchService.SearchMovieIds(search);
                        filter = filter.Where(x => movieIds.Contains(x.M.ID));
                    }
                    else
                    {
                        filter = filter.Where(x => x.M.Title.Contains(search.Search)
                                               || x.M.OriginalTitle.Contains(search.Search));
                    }
                }

                if (search.WaitList)
                {
                    filter = filter.Where(x => x.MuWaitList);
                }

                if (search.KinopoiskID > 0)
                {
                    filter = filter.Where(x => x.M.KinopoiskID == search.KinopoiskID);
                }

                if (search.SystemList)
                {
                    var candidates = from mu in db.MovieUser
                                     join mr in db.MovieRecommendation on mu.MovieID equals mr.MovieID
                                     from omu in db.MovieUser.Where(mm => mm.MovieID == mr.OtherMovieID && mm.UserID == userId).DefaultIfEmpty()
                                     where mu.UserID == userId && mu.Mark == 10
                                           && Sql2.IsNullOrFalse(omu.IsDontWant)
                                           && Sql2.IsNullOrFalse(omu.IsWaitlist)
                                           && Sql2.IsNullOrFalse(omu.IsWatched)
                                     orderby mr.Position
                                     select (int?)mr.OtherMovieID;

                    var rmovies = candidates
                        .Take(50)
                        .ToArray();

                    if (rmovies.Length > 0)
                    {
                        filter = filter.Where(x => rmovies.Contains(x.M.ID));
                    }
                    else
                    {
                        filter = filter.Where(x => (x.M.Status == MovieStatus.ComingSoon || x.M.Status == MovieStatus.RecentPremiere)
                                                && Sql2.IsNullOrFalse(x.MuUserWatched)
                                                && Sql2.IsNullOrFalse(x.MuWaitList)
                                                && Sql2.IsNullOrFalse(x.MuDontWant)
                                                );
                    }
                }

                if (search.MovieStatus != MovieStatus.Unknown)
                {
                    filter = filter.Where(x => x.M.Status == search.MovieStatus);
                }

                List<Person> actors = null;

                if (!string.IsNullOrEmpty(search.Actors))
                {
                    var actorIds = search.Actors
                        .Split(',')
                        .Select(x => Convert.ToInt32(x))
                        .Cast<int?>()
                        .ToArray();

                    actors = db.Person
                        .Where(p => actorIds.Contains(p.ID))
                        .ToList();

                    var movieIds = db.MoviePerson
                        .Where(mp => actorIds.Contains(mp.PersonID))
                        .Select(mp => mp.MovieID)
                        .Cast<int?>()
                        .ToArray();

                    filter = filter.Where(x => movieIds.Contains(x.M.ID));
                }

                switch (pageParams.Order)
                {
                    case "wldate":
                        filter = filter.OrderByDescending(x => x.WlDate);
                        break;

                    case "rkp":
                        filter = filter.OrderByDescending(f => f.M.RatingKinopoisk);
                        break;

                    case "rimdb":
                        filter = filter.OrderByDescending(f => f.M.RatingImdb);
                        break;

                    case "ruser":
                        filter = filter
                            .Where(x => x.MuMark != null)
                            .OrderByDescending(f => f.MuMark)
                            .ThenByDescending(f => f.M.RatingKinopoisk);
                        break;

                    case "quality":
                        filter = filter
                            .OrderByDescending(f => f.M.BestVideoQuality)
                            .ThenByDescending(f => f.M.RatingKinopoisk);
                        break;

                    case "added":
                        filter = filter
                            .OrderByDescending(f => f.M.Created)
                            .ThenByDescending(f => f.M.ID);
                        break;

                    case "topweek":
                        // ReSharper disable ConditionIsAlwaysTrueOrFalse
                        filter = filter
                            .Where(x => x.M.MovieTopWeek.TorrentCount != null)
                            .Where(x => Sql2.IsNullOrFalse(x.MuUserWatched)
                                        && Sql2.IsNullOrFalse(x.MuWaitList)
                                        && Sql2.IsNullOrFalse(x.MuDontWant))
                            .OrderByDescending(f => f.M.MovieTopWeek.TorrentCount);
                        // ReSharper restore ConditionIsAlwaysTrueOrFalse
                        break;

                    default:
                        filter = filter.OrderBy(t => t.M.Title);
                        break;
                }

                var count = pageParams.NoCount ? 0 : filter.Count();
                var movies = filter
                    .Skip(pageParams.SkipCount)
                    .Take(pageParams.PageSize)
                    .Select(t => new MovieItem
                    {
                        Self = new Movie
                        {
                            ID = t.M.ID,
                            Title = t.M.Title,
                            OriginalTitle = t.M.OriginalTitle,
                            ImdbID = t.M.ImdbID,
                            KinopoiskID = t.M.KinopoiskID,
                            RatingImdb = t.M.RatingImdb,
                            RatingKinopoisk = t.M.RatingKinopoisk,
                            ReleaseDate = t.M.ReleaseDate,
                            Status = t.M.Status,
                            BestVideoQuality = t.M.BestVideoQuality
                        },
                        InWaitList = t.MuWaitList,
                        Mark = t.MuMark,
                        IsWatched = t.MuUserWatched,
                        IsDontWant = t.MuDontWant
                    })
                    .ToList();

                return new MoviePagination(movies)
                {
                    PageSize = pageParams.PageSize,
                    TotalItems = count,
                    Actors = actors
                };
            }
        }

        public void AddMovieUserLink(int movieId, int? userId, bool? waitlist, bool? watched, int? mark, bool? dontwant)
        {
            if (userId == null) return;

            using (var db = new DbTorronto())
            {
                var newMark = _marks
                    .TakeWhile(x => x <= mark)
                    .LastOrDefault();

                var update = db.MovieUser
                    .Where(mu => mu.UserID == userId && mu.MovieID == movieId)
                    .AsUpdatable();

                if (waitlist == true)
                {
                    update = update.Set(f => f.IsWaitlist, true);
                }

                if (watched == true)
                {
                    update = update
                        .Set(f => f.IsWatched, true)
                        .Set(f => f.IsWaitlist, false)
                        .Set(f => f.IsDontWant, false);
                }

                if (dontwant == true)
                {
                    update = update
                        .Set(f => f.IsDontWant, true)
                        .Set(f => f.IsWatched, false)
                        .Set(f => f.IsWaitlist, false)
                        .Set(f => f.Mark, (int?)null);
                }

                if (newMark > 0)
                {
                    update = update
                        .Set(f => f.Mark, newMark)
                        .Set(f => f.IsWatched, true)
                        .Set(f => f.IsWaitlist, false)
                        .Set(f => f.IsDontWant, false);
                }

                var affected = update.Update();

                if (affected == 0)
                {
                    var newMu = new MovieUser
                    {
                        Created = DateTime.UtcNow,
                        UserID = userId.GetValueOrDefault(),
                        MovieID = movieId
                    };

                    if (waitlist == true) newMu.IsWaitlist = true;
                    if (watched == true) newMu.IsWatched = true;
                    if (dontwant == true) newMu.IsDontWant = true;
                    if (newMark > 0)
                    {
                        newMu.Mark = newMark;
                        newMu.IsWatched = true;
                    }

                    db.Insert(newMu);
                }
            }
        }

        public void RemoveMovieUserLink(int movieId, int? userId, bool? waitlist = null, bool? watched = null, bool? dontwant = null)
        {
            using (var db = new DbTorronto())
            {
                var update = db.MovieUser
                    .Where(mu => mu.UserID == userId && mu.MovieID == movieId)
                    .Set(f => f.MovieID, movieId);

                if (waitlist == true)
                {
                    update = update.Set(f => f.IsWaitlist, false);
                }

                if (watched == true)
                {
                    update = update
                        .Set(f => f.IsWatched, false)
                        .Set(f => f.Mark, (int?)null);
                }

                if (dontwant == true)
                {
                    update = update
                        .Set(f => f.IsDontWant, false);
                }

                update.Update();
            }
        }

        public MovieItem GetMovieSingle(int? userId, int movieId)
        {
            using (var db = new DbTorronto())
            {
                var item = db.Movie
                    .NoCopyrighted()
                    .SelectMany(m => db.MovieUser.Where(x => x.MovieID == m.ID && x.UserID == userId).DefaultIfEmpty(),
                        (m, movieUser) => new { movie = m, muUser = movieUser })
                    .FirstOrDefault(x => x.movie.ID == movieId);

                if (item != null)
                {
                    var persons = db.MoviePerson
                        .Where(mp => mp.MovieID == movieId)
                        .OrderBy(mp => mp.Position)
                        .Select(mp => mp.Person);

                    var genres = db.MovieGenre
                        .Where(mg => mg.MovieID == movieId)
                        .OrderBy(mg => mg.Position)
                        .Select(mg => mg.Genre);

                    Movie.AddRelated(item.movie, persons.Take(6));
                    Movie.AddRelated(item.movie, genres);

                    var movieItem = new MovieItem
                    {
                        Self = item.movie
                    };

                    if (item.muUser != null)
                    {
                        movieItem.InWaitList = item.muUser.IsWaitlist;
                        movieItem.IsWatched = item.muUser.IsWatched;
                        movieItem.Mark = item.muUser.Mark;
                        movieItem.IsDontWant = item.muUser.IsDontWant;
                    }

                    return movieItem;
                }
            }

            return null;
        }

        public Movie GetById(int movieId)
        {
            using (var db = new DbTorronto())
            {
                return db.Movie
                    .NoCopyrighted()
                    .FirstOrDefault(m => m.ID == movieId);
            }
        }

        public void SaveDetails(Movie detailed)
        {
            using (var db = new DbTorronto())
            {
                db.Movie
                    .Where(x => x.ID == detailed.ID)
                    .Set(f => f.IsDetailed, detailed.IsDetailed)
                    .Set(f => f.Title, detailed.Title)
                    .Set(f => f.OriginalTitle, detailed.OriginalTitle)
                    .Set(f => f.ReleaseDate, detailed.ReleaseDate)
                    .Set(f => f.Description, detailed.Description)
                    .Set(f => f.ImdbID, detailed.ImdbID)
                    .Set(f => f.DurationMinutes, detailed.DurationMinutes)
                    .Update();

                var actorPosition = 0;

                foreach (var actor in detailed.Persons)
                {
                    var existingActor = db.Person.FirstOrDefault(p => p.SiteID == actor.SiteID);

                    if (existingActor == null)
                    {
                        existingActor = new Person
                        {
                            Name = actor.Name,
                            SiteID = actor.SiteID
                        };

                        existingActor.ID = Convert.ToInt32(
                            db.InsertWithIdentity(existingActor)
                            );
                    }

                    db.MoviePerson.InsertOrUpdate(
                        () => new MoviePerson
                        {
                            MovieID = detailed.ID.GetValueOrDefault(),
                            PersonID = existingActor.ID.GetValueOrDefault(),
                            Position = actorPosition
                        },
                        old => new MoviePerson
                        {
                            Position = actorPosition
                        }
                        );

                    actorPosition++;
                }

                var genrePosition = 0;

                foreach (var genre in detailed.Genres)
                {
                    var existingGenre = db.Genre.FirstOrDefault(p => p.SiteID == genre.SiteID);

                    if (existingGenre == null)
                    {
                        existingGenre = new Genre
                        {
                            Name = genre.Name,
                            SiteID = genre.SiteID
                        };

                        existingGenre.ID = Convert.ToInt32(
                            db.InsertWithIdentity(existingGenre)
                            );
                    }

                    db.MovieGenre.InsertOrUpdate(
                        () => new MovieGenre
                        {
                            MovieID = detailed.ID.GetValueOrDefault(),
                            GenreID = existingGenre.ID.GetValueOrDefault(),
                            Position = genrePosition
                        },
                        old => new MovieGenre
                        {
                            Position = genrePosition
                        }
                        );

                    genrePosition++;
                }
            }
        }

        public void SaveMovies(List<Movie> movies, int? userId)
        {
            using (var db = new DbTorronto())
            {
                foreach (var movie in movies.Where(m => m.KinopoiskID != null))
                {
                    var existing = db.Movie
                        .FirstOrDefault(x => x.KinopoiskID == movie.KinopoiskID);

                    int movieId;

                    if (existing == null)
                    {
                        movieId = CreateMovie(db, movie);

                        if (movieId == 0) continue;

                        QueueService.AddMovieForMatch(movieId);
                    }
                    else
                    {
                        movieId = existing.ID.GetValueOrDefault();

                        db.Movie
                            .Where(x => x.ID == existing.ID)
                            .Set(f => f.Status, movie.Status)
                            .Set(f => f.Updated, DateTime.UtcNow)
                            .Update();
                    }

                    movie.ID = movieId;

                    if (userId != null && !db.MovieUser.Any(x => x.MovieID == movieId && x.UserID == userId))
                    {
                        db.InsertMovieUser(movieId, userId.GetValueOrDefault());
                    }
                }
            }
        }

        public int CreateMovie(DbTorronto db, Movie movie)
        {
            int movieId = 0;

            movie.Created = DateTime.UtcNow;
            movie.Updated = DateTime.UtcNow;

            try
            {
                movieId = Convert.ToInt32(
                    db.InsertWithIdentity(movie)
                    );
            }
            catch (Exception ex)
            {
                _logger.Warn("MovieService.CreateMovie", ex);
            }

            return movieId;
        }

        public List<Movie> CreateMoviesFromTorrents()
        {
            List<Movie> movies;

            using (var db = new DbTorronto())
            {
                var ids = db.Torrent
                    .Where(t => t.MovieID == null && t.KinopoiskID != null)
                    .Select(t => t.KinopoiskID)
                    .Distinct()
                    .Take(100)
                    .ToList();

                movies = ids
                    .Where(id => db.Movie.FirstOrDefault(m => m.KinopoiskID == id) == null)
                    .Select(kinopoiskID => new Movie
                    {
                        Title = "Unknown yet",
                        KinopoiskID = kinopoiskID
                    })
                    .ToList();
            }

            SaveMovies(movies, null);

            return movies;
        }
    }
}