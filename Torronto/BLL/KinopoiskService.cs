using CsQuery;
using LinqToDB;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Torronto.BLL.Ext;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.BLL
{
    public class KinopoiskService
    {
        private readonly MovieService _movieService;
        private static readonly int _undetailedMoviesLimit = Convert.ToInt32(ConfigurationManager.AppSettings["Kinopoisk.UndetailedMoviesLimit"]);

        private static readonly Regex _movieIdRegex = new Regex(@"top_film_(\d+)");
        private static readonly Regex _movieIdHrefRegex = new Regex(@"/film/(\d+)");
        private static readonly Regex _actorSiteIDRegex = new Regex(@"/name/(\d+)/");
        private static readonly Regex _genreSiteIDRegex = new Regex(@"/lists/m_act[^/]+/(\d+)/");
        private static readonly Regex _durationMinutesRegex = new Regex(@"(\d+) мин.");

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public KinopoiskService(MovieService movieService)
        {
            _movieService = movieService;
        }

        public void ParseFeed()
        {
            using (var client = GetClient())
            {
                var comingSoonResponse = client.GetAsync("comingsoon/").Result;
                if (comingSoonResponse.IsSuccessStatusCode)
                {
                    var feed = comingSoonResponse.Content.ReadAsStringAsync().Result;
                    var movies = GetMoviesComingSoon(feed);

                    _movieService.SaveMovies(movies, null);
                }

                Thread.Sleep(2000);

                var lastPremiereResponse = client.GetAsync("top/lists/222/").Result;
                if (lastPremiereResponse.IsSuccessStatusCode)
                {
                    var feed = lastPremiereResponse.Content.ReadAsStringAsync().Result;
                    var movies = GetLastPremiere(feed);

                    _movieService.SaveMovies(movies, null);
                }

                Thread.Sleep(2000);

                _movieService.CreateMoviesFromTorrents();
                ProcessDetails();
            }
        }

        private List<Movie> GetMoviesComingSoon(string feed)
        {
            var movies = new List<Movie>(100);
            var dom = CQ.Create(feed, Encoding.UTF8);
            var movieItems = dom[".coming_films .item"];

            foreach (var item in movieItems)
            {
                var cq = item.Cq();
                var idMatch = _movieIdRegex.Match(item.GetAttribute("id"));
                var kinopoiskId = idMatch.Success ? Convert.ToInt32(idMatch.Groups[1].Value) : (int?)null;
                var title = cq.Find(".info .name a").Select(x => x.InnerText).FirstOrDefault();

                movies.Add(new Movie
                {
                    KinopoiskID = kinopoiskId,
                    Title = title,
                    Status = MovieStatus.ComingSoon
                });
            }

            return movies;
        }

        private List<Movie> GetLastPremiere(string feed)
        {
            var movies = new List<Movie>(100);
            var dom = CQ.Create(feed, Encoding.UTF8);
            var movieItems = dom["#itemList td.news a.all"];

            foreach (var item in movieItems)
            {
                var hrefMatch = _movieIdHrefRegex.Match(item.GetAttribute("href"));
                var kinopoiskId = hrefMatch.Success ? Convert.ToInt32(hrefMatch.Groups[1].Value) : (int?)null;
                var title = item.InnerText;

                movies.Add(new Movie
                {
                    KinopoiskID = kinopoiskId,
                    Title = title,
                    Status = MovieStatus.RecentPremiere
                });
            }

            return movies;
        }

        public void ProcessDetails()
        {
            List<int> unprocessed;
            using (var db = new DbTorronto())
            {
                unprocessed = db.Movie
                    .Where(x => !x.IsDetailed)
                    .OrderByDescending(x => x.Created)
                    .Take(_undetailedMoviesLimit)
                    .Select(x => x.ID.GetValueOrDefault())
                    .ToList();
            }

            foreach (var movieId in unprocessed)
            {
                QueueService.AddMovieForDetails(movieId);
                QueueService.AddMovieForRating(movieId);
            }
        }

        public void ProcessDetailsSingle(int movieId)
        {
            _logger.Info("Detailing movie #{0}", movieId);

            Movie movie;
            using (var db = new DbTorronto())
            {
                movie = db.Movie.FirstOrDefault(m => m.ID == movieId);
            }

            if (movie == null)
            {
                _logger.Warn("No such movie #{0}", movieId);
                return;
            }

            if (movie.IsDetailed)
            {
                _logger.Trace("Already detailed movie #{0}", movieId);
                return;
            }

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Referrer = new Uri("http://www.kinopoisk.ru/film/" + movie.KinopoiskID);

                var response = client.GetAsync("film/" + movie.KinopoiskID).Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;

                    File.WriteAllText(Path.Combine("html", "kinopoisk", movieId + ".html"), content);

                    if (content.Contains("www.google.com/recaptcha/api/challenge"))
                    {
                        throw new Exception("Captcha found");
                    }

                    var dom = CQ.Create(content, Encoding.UTF8);
                    var title = dom
                        .Find(@"#headerFilm h1[itemprop=name]")
                        .Select(x => x.InnerText)
                        .FirstOrDefault();
                    var originalTitle = dom
                        .Find(@"#headerFilm span[itemprop=alternativeHeadline]")
                        .Select(x => x.InnerText)
                        .FirstOrDefault();
                    var release = dom
                        .Find(@"#div_world_prem_td2 .prem_ical")
                        .Select(x => x.GetAttribute("data-date-premier-start-link"))
                        .FirstOrDefault();
                    var description = dom
                        .Find(@".brand_words[itemprop=description]")
                        .Select(x => x.InnerHTML)
                        .FirstOrDefault();
                    var actors = dom
                        .Find(@"#actorList a")
                        .Select(x =>
                        {
                            var attribute = x.GetAttribute("href") ?? string.Empty;
                            var rr = _actorSiteIDRegex.Match(attribute);
                            return new Person
                            {
                                Name = x.InnerText,
                                SiteID = rr.Success ? Convert.ToInt32(rr.Groups[1].Value) : 0
                            };
                        })
                        .TakeWhile(x => x.Name != "...")
                        .Where(x => x.SiteID != 0);
                    var genres = dom
                        .Find(@"span[itemprop=genre] a")
                        .Select(x =>
                        {
                            var attribute = x.GetAttribute("href") ?? string.Empty;
                            var rr = _genreSiteIDRegex.Match(attribute);
                            return new Genre
                            {
                                Name = x.InnerText,
                                SiteID = rr.Success ? Convert.ToInt32(rr.Groups[1].Value) : 0
                            };
                        })
                        .Where(x => x.SiteID != 0);
                    var duration =
                        dom
                        .Find(@"#runtime")
                        .Select(x =>
                        {
                            var r = _durationMinutesRegex.Match(x.InnerHTML ?? string.Empty);

                            return r.Success ? Convert.ToInt32(r.Groups[1].Value) : 0;
                        })
                        .FirstOrDefault();

                    DateTime? releaseDate = null;

                    if (release != null)
                    {
                        var year = Convert.ToInt32(release.Substring(0, 4));
                        var month = Convert.ToInt32(release.Substring(4, 2));
                        var day = Convert.ToInt32(release.Substring(6, 2));

                        if (year > 0 && month > 0 && day > 0)
                        {
                            releaseDate = new DateTime(year, month, day);
                        }
                    }

                    int? imdbId = null;

                    if (movie.ImdbID == null)
                    {
                        imdbId = TryGetImdbId(originalTitle, releaseDate);

                        if (imdbId != null)
                        {
                            _logger.Info("movie #{0}, imdb = {1}", movie.ID, imdbId);
                        }
                    }

                    movie.IsDetailed = true;
                    movie.Title = title;
                    movie.OriginalTitle = originalTitle;
                    movie.ReleaseDate = releaseDate;
                    movie.Description = description;
                    movie.ImdbID = imdbId;
                    movie.DurationMinutes = duration;

                    Movie.AddRelated(movie, actors);
                    Movie.AddRelated(movie, genres);

                    _movieService.SaveDetails(movie);

                    QueueService.AddMovieForReindex(movieId);
                    QueueService.AddMovieForRecommendation(movieId);
                }
            }
        }

        public void UpdateRatings()
        {
            List<int> unprocessed;
            using (var db = new DbTorronto())
            {
                unprocessed = db.Movie
                    .Where(x => x.RatingLastGather == null || x.RatingLastGather < DateTime.UtcNow.AddDays(-14))
                    .Take(500)
                    .Select(x => x.ID.GetValueOrDefault())
                    .ToList();
            }

            foreach (var movieId in unprocessed)
            {
                QueueService.AddMovieForRating(movieId);
            }
        }

        public void UpdateRecommendations()
        {
            List<int> unprocessed;
            using (var db = new DbTorronto())
            {
                unprocessed = db.Movie
                    .Where(x => x.LastRecommendation == null || x.LastRecommendation < DateTime.UtcNow.AddMonths(-6))
                    .Take(_undetailedMoviesLimit)
                    .Select(x => x.ID.GetValueOrDefault())
                    .ToList();
            }

            foreach (var movieId in unprocessed)
            {
                QueueService.AddMovieForRecommendation(movieId);
            }
        }

        public void UpdateMovieRating(int movieId)
        {
            _logger.Info("Updating rating for movie #{0}", movieId);

            Movie movie;
            using (var db = new DbTorronto())
            {
                movie = db.Movie.FirstOrDefault(m => m.ID == movieId);
            }

            if (movie == null)
            {
                _logger.Warn("No such movie #{0}", movieId);
                return;
            }

            using (var client = GetClient())
            {
                var url = $"http://rating.kinopoisk.ru/{movie.KinopoiskID}.xml";

                var ratingResponse = client.GetAsync(url).Result;
                if (ratingResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        var ratingXml = ratingResponse.Content.ReadAsStringAsync().Result;
                        var doc = XDocument.Load(new StringReader(ratingXml));
                        var ratings = doc
                            .Descendants("rating")
                            .Select(delegate(XElement r)
                            {
                                var kpRating = r.Element("kp_rating");
                                var imdbRating = r.Element("imdb_rating");

                                return new
                                {
                                    Kinopoisk = kpRating != null ? Convert.ToDecimal(kpRating.Value) : 0,
                                    Imdb = imdbRating != null ? Convert.ToDecimal(imdbRating.Value) : 0
                                };
                            })
                            .FirstOrDefault();

                        if (ratings != null)
                        {
                            using (var db = new DbTorronto())
                            {
                                db.Movie
                                    .Where(m => m.ID == movie.ID)
                                    .Set(f => f.RatingKinopoisk, ratings.Kinopoisk)
                                    .Set(f => f.RatingImdb, ratings.Imdb)
                                    .Set(f => f.RatingLastGather, DateTime.UtcNow)
                                    .Update();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("RATING", ex);
                    }
                }
            }
        }

        private class OmdbResponse
        {
            public string imdbID { get; set; }
            public string Type { get; set; }
            public string Genre { get; set; }
        }

        //todo fix this hacky data retrieval from unreliable source

        public int? TryGetImdbId(string originalTitle, DateTime? releaseDate)
        {
            if (string.IsNullOrEmpty(originalTitle) || releaseDate == null) return null;

            using (var client = new HttpClient { BaseAddress = new Uri("http://www.omdbapi.com/") })
            {
                var url = string.Format("?t={0}&y={1}", Uri.EscapeDataString(originalTitle), releaseDate.Value.Year);
                var response = client.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                {
                    var s = response.Content.ReadAsStringAsync().Result;
                    var data = JsonConvert.DeserializeObject<OmdbResponse>(s);

                    if (data != null && data.imdbID != null)
                    {
                        if (data.Type != "movie") return null;
                        if (data.Genre.ToLower().Contains("short")) return null;

                        int r;
                        if (int.TryParse(data.imdbID.Trim('t'), out r)) return r;
                    }
                }
            }

            return null;
        }

        public void GetRecommendations(int movieId)
        {
            _logger.Info("GetRecommendations for movie #{0}", movieId);

            Movie movie;
            using (var db = new DbTorronto())
            {
                movie = db.Movie.FirstOrDefault(m => m.ID == movieId);
            }

            if (movie == null)
            {
                _logger.Warn("No such movie #{0}", movieId);
                return;
            }

            if (movie.LastRecommendation != null
                && movie.LastRecommendation > DateTime.UtcNow.AddMonths(-6))
            {
                return;
            }

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.1; rv:33.0) Gecko/20100101 Firefox/33.0");
                client.DefaultRequestHeaders.Referrer = new Uri("http://www.kinopoisk.ru/film/" + movie.KinopoiskID);

                var response = client.GetAsync("film/" + movie.KinopoiskID + "/like").Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var dom = CQ.Create(content, Encoding.UTF8);
                    var movieItems = dom["td.news a.all"];
                    var otherMovies = movieItems
                        .Select(item =>
                        {
                            var hrefMatch = _movieIdHrefRegex.Match(item.GetAttribute("href"));
                            var title = item.InnerText;

                            return new Movie
                            {
                                KinopoiskID = hrefMatch.Success ? Convert.ToInt32(hrefMatch.Groups[1].Value) : (int?)null,
                                Title = title ?? "Unknown yet"
                            };
                        })
                        .Where(m => m.KinopoiskID != null)
                        .Take(10);

                    using (var db = new DbTorronto())
                    {
                        var otherMoviePosition = 0;

                        foreach (var otherMovie in otherMovies)
                        {
                            var existingMovie = db.Movie
                                .FirstOrDefault(m => m.KinopoiskID == otherMovie.KinopoiskID);

                            if (existingMovie == null)
                            {
                                existingMovie = new Movie
                                {
                                    KinopoiskID = otherMovie.KinopoiskID,
                                    Title = otherMovie.Title
                                };

                                existingMovie.ID = _movieService.CreateMovie(db, existingMovie);

                                QueueService.AddMovieForDetails(existingMovie.ID.GetValueOrDefault());
                            }

                            db.MovieRecommendation.InsertOrUpdate(
                                () => new MovieRecommendation
                                {
                                    MovieID = movie.ID.GetValueOrDefault(),
                                    OtherMovieID = existingMovie.ID.GetValueOrDefault(),
                                    Position = otherMoviePosition
                                },
                                old => new MovieRecommendation
                                {
                                    Position = otherMoviePosition
                                });

                            otherMoviePosition++;
                        }

                        db.Movie
                            .Where(m => m.ID == movie.ID)
                            .Set(f => f.LastRecommendation, DateTime.UtcNow)
                            .Update();
                    }
                }
            }
        }

        private HttpClient GetClient()
        {
            var baseUrl = new Uri("http://www.kinopoisk.ru/");
            var cookieContainer = new CookieContainer();
            var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,               
                //todo make this configurable
                Proxy = new WebProxy("http://127.0.0.1:8123"),
                UseProxy = true
            };

            var client = new HttpClient(httpClientHandler)
            {
                BaseAddress = baseUrl
            };

            client.DefaultRequestHeaders.Referrer = baseUrl;
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "max-age=0");

            return client;
        }
    }
}