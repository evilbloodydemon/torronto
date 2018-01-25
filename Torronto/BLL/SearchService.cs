using LinqToDB.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Torronto.BLL.Models;
using Torronto.DAL;
using Torronto.DAL.Models;
using Torronto.Lib.Sphinx;

namespace Torronto.BLL
{
    public class SearchService
    {
        private const string _originalLayout = "qwertyuiop[]asdfghjkl;'zxcvbnm,./йцукенгшщзхъфывапролджэячсмитьбю.";
        private const string _switchedLayout = "йцукенгшщзхъфывапролджэячсмитьбю.qwertyuiop[]asdfghjkl;'zxcvbnm,./";

        public int ReindexTorrents()
        {
            var total = 0;

            using (var sphinx = new DbSphinx())
            {
                sphinx.Execute("TRUNCATE RTINDEX torronto_torrents");
            }

            using (var db = new DbTorronto())
            {
                var page = 0;
                var count = 0;
                var pageSize = 100;

                do
                {
                    var torrents = db.Torrent
                        .OrderBy(t => t.ID)
                        .Skip(pageSize * page)
                        .Take(pageSize)
                        .ToList();

                    count = torrents.Count;
                    total += count;

                    foreach (var torrent in torrents)
                    {
                        IndexTorrent(torrent);
                    }

                    page++;
                } while (count > 0);
            }

            return total;
        }

        public void IndexTorrent(Torrent torrent)
        {
            using (var sphinx = new DbSphinx())
            {
                var query = new SphinxReplaceBuilder("torronto_torrents")
                    .AddField("id", torrent.ID.GetValueOrDefault())
                    .AddField("title", torrent.Title)
                    .AddField("video_quality", (int)torrent.VideoQuality)
                    .AddField("sound_quality", (int)torrent.AudioQuality)
                    .AddField("translation", (int)torrent.Translation)
                    .AddField("size", decimal.ToInt32(torrent.Size))
                    .Build();

                sphinx.Execute(query);
            }
        }

        public IEnumerable<int?> SearchTorrentIds(TorrentSearchParams search)
        {
            if (string.IsNullOrEmpty(search.Search)) return Enumerable.Empty<int?>();

            using (var sphinx = new DbSphinx())
            {
                var qb = new SphinxQueryBuilder("torronto_torrents")
                    .SelectColumns("id")
                    .SelectLiteral(string.Format("((video_quality & {0}) > 0) vq", (int)search.VideoQuality))
                    .SelectLiteral(string.Format("((sound_quality & {0}) > 0) aq", (int)search.AudioQuality))
                    .SelectLiteral(string.Format("((translation & {0}) > 0) tq", (int)search.TranslationQuality))
                    .AddMatch(EscapeUserInput(search.Search))
                    .AddLimits(0, 100);

                if (search.VideoQuality > VideoQuality.Unknown) qb.AddWhere("vq", 1);
                if (search.AudioQuality > AudioQuality.Unknown) qb.AddWhere("aq", 1);
                if (search.TranslationQuality > Translation.Unknown) qb.AddWhere("tq", 1);

                var items = sphinx.Query<int?>(qb.Build())
                    .ToList();

                return items;
            }
        }

        public int ReindexMovies()
        {
            var total = 0;

            using (var sphinx = new DbSphinx())
            {
                sphinx.Execute("TRUNCATE RTINDEX torronto_movies");
            }

            using (var db = new DbTorronto())
            {
                var page = 0;
                var count = 0;
                var pageSize = 100;

                do
                {
                    var movies = db.Movie
                        .NoCopyrighted()
                        .OrderBy(movie => movie.ID)
                        .Skip(pageSize * page)
                        .Take(pageSize)
                        .ToList();

                    count = movies.Count;
                    total += count;

                    foreach (var movie in movies)
                    {
                        IndexMovie(movie);
                    }

                    page++;
                } while (count > 0);
            }

            return total;
        }

        public void IndexMovie(Movie movie)
        {
            using (var sphinx = new DbSphinx())
            {
                var query = new SphinxReplaceBuilder("torronto_movies")
                    .AddField("id", movie.ID.GetValueOrDefault())
                    .AddField("title", movie.Title)
                    .AddField("original_title", movie.OriginalTitle)
                    .AddField("description", movie.Description)
                    .AddField("status", (int)movie.Status)
                    .Build();

                var complTitle = new StringBuilder(movie.Title);

                if (!string.IsNullOrEmpty(movie.OriginalTitle)) complTitle.AppendFormat(" / {0}", movie.OriginalTitle);
                if (movie.ReleaseDate.HasValue) complTitle.AppendFormat(" ({0})", movie.ReleaseDate.Value.Year);

                var complQuery = new SphinxReplaceBuilder("torronto_movies_completion")
                    .AddField("id", movie.ID.GetValueOrDefault())
                    .AddField("title", complTitle.ToString())
                    .AddField("title_indexed", complTitle.ToString())
                    .Build();

                sphinx.Execute(query);
                sphinx.Execute(complQuery);
            }
        }

        public IEnumerable<int?> SearchMovieIds(MovieSearchParams search)
        {
            if (string.IsNullOrEmpty(search.Search)) return Enumerable.Empty<int?>();

            using (var sphinx = new DbSphinx())
            {
                var qb = new SphinxQueryBuilder("torronto_movies")
                    .SelectColumns("id")
                    .AddMatch(string.Format("@!(description) {0}", EscapeUserInput(search.Search)))
                    .AddLimits(0, 100);

                if (search.MovieStatus > MovieStatus.Unknown) qb.AddWhere("status", (int)search.MovieStatus);

                var items = sphinx.Query<int?>(qb.Build())
                    .ToList();

                return items;
            }
        }

        public IEnumerable<Movie> MovieCompletion(string title)
        {
            using (var sphinx = new DbSphinx())
            {
                var query = BuildCompleteionQuery(title);
                var movies = sphinx.Query<Movie>(query).ToList();

                if (movies.Count == 0)
                {
                    var newTitle = title
                        .ToLower()
                        .Select(c =>
                        {
                            var pos = _originalLayout.IndexOf(c);
                            return pos > -1 ? _switchedLayout[pos] : ' ';
                        });

                    var trQuery = BuildCompleteionQuery(string.Concat(newTitle));

                    movies = sphinx.Query<Movie>(trQuery).ToList();
                }

                return movies;
            }
        }

        private string BuildCompleteionQuery(string title)
        {
            return new SphinxQueryBuilder("torronto_movies_completion")
                .SelectColumns("id", "title")
                .AddMatch(EscapeUserInput(title) + "*")
                .AddLimits(0, 5)
                .Build();
        }

        private string EscapeUserInput(string input)
        {
            return Regex.Replace(input, @"([=\(\)|\-!@~""&/\\^\$\=])", " ");
        }
    }
}