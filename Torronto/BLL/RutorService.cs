using CsQuery;
using LinqToDB;
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
using CsQuery.Output;
using Torronto.BLL.Detector;
using Torronto.BLL.Ext;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.BLL
{
    public class RutorService
    {
        private readonly TorrentService _torrentService;
        private readonly IQualityDetector _qualityDetector;

        private static readonly Regex _imdbRegex = new Regex(@"imdb.com/title/tt(\d+)");
        private static readonly Regex _kinopoiskRegex = new Regex(@"kinopoisk.ru/.*film/+(\d+)");
        private static readonly Regex _magnetRegex = new Regex(@"magnet:\?xt=urn:btih:([a-z0-9]+)");

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static readonly string _rutorDomain = ConfigurationManager.AppSettings["Rutor.Domain"];
        private static readonly int _scanPages = Convert.ToInt32(ConfigurationManager.AppSettings["Rutor.ScanPages"]);
        private static readonly int _undetailedTorrentsLimit = Convert.ToInt32(ConfigurationManager.AppSettings["Rutor.UndetailedTorrentsLimit"]);

        private static readonly string[] _months =
        {
            "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек"
        };

        public RutorService(
            TorrentService torrentService,
            QualityDetector qualityDetector
        )
        {
            _torrentService = torrentService;
            _qualityDetector = qualityDetector;
        }

        public void ParseFeed()
        {
            var feedUris = new[]
            {
                new {Uri = "browse/{0}/1/0/0", Category = TorrentCategory.Foreign},
                new {Uri = "browse/{0}/5/0/0", Category = TorrentCategory.Russian},
                new {Uri = "browse/{0}/4/0/0", Category = TorrentCategory.TvSeries},
                new {Uri = "browse/{0}/7/0/0", Category = TorrentCategory.Cartoons},
            };

            foreach (var entry in feedUris)
            {
                using (var client = GetClient())
                {
                    for (var page = 0; page < _scanPages; page++)
                    {
                        var uri = string.Format(entry.Uri, page);

                        _logger.Trace("RUTOR loading url '{0}'", uri);

                        var response = client.GetAsync(uri).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            var feed = response.Content.ReadAsStringAsync().Result;
                            var dom = CQ.Create(feed, Encoding.UTF8);
                            var trs = dom["#index tr"];
                            var torrents = GetTorrentsList(trs, entry.Category);

                            _torrentService.SaveToDb(torrents);
                        }

                        Thread.Sleep(4000);
                    }
                }
            }

            ProcessDetails();
        }

        private void ProcessDetails()
        {
            List<int> unprocessed;
            using (var db = new DbTorronto())
            {
                //todo move this to proper place
                db.RefreshTopWeekMovies();

                unprocessed = db.Torrent
                    .Where(x => !x.IsDetailed)
                    .OrderByDescending(x => x.Created)
                    .Take(_undetailedTorrentsLimit)
                    .Select(x => x.ID.GetValueOrDefault())
                    .ToList();
            }

            foreach (var torrentId in unprocessed)
            {
                QueueService.AddTorrentForDetails(torrentId);
            }
        }

        public void ProcessDetailsSingle(int torrentId)
        {
            _logger.Info("Detailing torrent #{0}", torrentId);

            Torrent torrent;
            using (var db = new DbTorronto())
            {
                torrent = db.Torrent.First(t => t.ID == torrentId);
            }

            if (torrent == null)
            {
                _logger.Warn("No such torrent #{0}", torrentId);
                return;
            }

            using (var client = GetClient())
            {
                var response = client.GetAsync("torrent/" + torrent.SiteID).Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var dom = CQ.Create(content, Encoding.UTF8);

                    var links = dom["#details a"];
                    var title = (dom["title"].Select(x => x.InnerText).FirstOrDefault() ?? string.Empty).ToLower();

                    int? imdbId, kinopoiskId;

                    FillExternalMovieIds(links, out imdbId, out kinopoiskId);

                    var quality = _qualityDetector.Detect(dom);

                    if (torrent.Category == TorrentCategory.Russian)
                    {
                        quality.TranslationQuality = Translation.Dub;
                    }

                    torrent.ImdbID = imdbId;
                    torrent.KinopoiskID = kinopoiskId;
                    torrent.VideoQuality = quality.VideoQuality;
                    torrent.AudioQuality = quality.AudioQuality;
                    torrent.Translation = quality.TranslationQuality;

                    var isRemoved = title.Contains("не существует");

                    _torrentService.SaveDetails(torrent, isRemoved);

                    QueueService.AddTorrentForReindex(torrentId);
                    QueueService.AddTorrentForMatch(torrentId);

                    File.WriteAllText(Path.Combine("html", "torrents", torrentId + ".html"), content);
                }
            }
        }

        private static void FillExternalMovieIds(CQ links, out int? imdbId, out int? kinopoiskId)
        {
            imdbId = null;
            kinopoiskId = null;

            foreach (var a in links)
            {
                var href = a.GetAttribute("href");
                var imdbMatch = _imdbRegex.Match(href);
                var kinopoiskMatch = _kinopoiskRegex.Match(href);

                if (imdbMatch.Success)
                {
                    imdbId = Convert.ToInt32(imdbMatch.Groups[1].Value);
                }

                if (kinopoiskMatch.Success)
                {
                    kinopoiskId = Convert.ToInt32(kinopoiskMatch.Groups[1].Value);
                }
            }

            if (imdbId != null && kinopoiskId != null) return;

            using (var db = new DbTorronto())
            {
                if (kinopoiskId == null && imdbId != null)
                {
                    var tmpImdb = imdbId.Value;
                    var kinopoiskIds = db.Torrent
                        .Where(t => t.ImdbID == tmpImdb && t.KinopoiskID != null)
                        .Select(t => t.KinopoiskID)
                        .Distinct()
                        .ToList();

                    if (kinopoiskIds.Count == 1)
                    {
                        kinopoiskId = kinopoiskIds.First();
                        _logger.Info("KPRESTORED {0} -> {1}", imdbId, kinopoiskId);
                    }
                    else
                    {
                        _logger.Info("KPNOTRESTORED {0} -> {1}", imdbId, string.Join(", ", kinopoiskIds));
                    }
                }
            }
        }

        private List<Torrent> GetTorrentsList(CQ trs, TorrentCategory torrentCategory)
        {
            var torrents = new List<Torrent>(100);

            foreach (var tr in trs.Skip(1))
            {
                var cq = tr.Cq();
                var dateString = cq.Find("td:eq(0)")
                    .Select(x => x.InnerText)
                    .FirstOrDefault();
                var sizeSelector = cq.Find("td:eq(1)")
                    .Select(x => x.GetAttribute("colspan"))
                    .FirstOrDefault() == "2" ? "td:eq(2)" : "td:eq(3)";
                var size = cq.Find(sizeSelector)
                    .Select(x => x.InnerText)
                    .FirstOrDefault();
                var href = cq.Find("a:eq(2)")
                    .Select(x => x.GetAttribute("href"))
                    .FirstOrDefault() ?? string.Empty;
                var infoHashHref = cq.Find("a:eq(1)")
                    .Select(x => x.GetAttribute("href"))
                    .FirstOrDefault() ?? string.Empty;
                var infoHashMatch = _magnetRegex.Match(infoHashHref);

                var regex = new Regex(@"/torrent/(\d+)");
                var result = regex.Match(href);

                if (result.Success)
                {
                    var date = ParseDate(dateString);

                    var torrent = new Torrent
                    {
                        Created = date,
                        Title = cq.Find("a:eq(2)").Select(x => x.InnerText).FirstOrDefault(),
                        SiteID = int.Parse(result.Groups[1].Value),
                        Size = ParseSize(size),
                        InfoHash = infoHashMatch.Success ? infoHashMatch.Groups[1].Value : string.Empty,
                        Category = torrentCategory
                    };

                    torrents.Add(torrent);
                }
            }

            return torrents;
        }

        private DateTime ParseDate(string dateString)
        {
            if (!string.IsNullOrEmpty(dateString))
            {
                var regex = new Regex(@"(\d+)\s*(.*?)\s*(\d+)");
                var regexResult = regex.Match(dateString);

                if (regexResult.Success)
                {
                    var day = Convert.ToInt32(regexResult.Groups[1].Value);
                    var year = Convert.ToInt32("20" + regexResult.Groups[3].Value);
                    var monthString = regexResult.Groups[2].Value.ToLower();
                    var month = _months
                        .TakeWhile(m => m != monthString)
                        .Count() + 1;

                    return new DateTime(year, month, day);
                }
            }

            return DateTime.UtcNow;
        }

        private decimal ParseSize(string input)
        {
            var v = input.Split(null);
            decimal size;

            decimal.TryParse(v[0], out size);

            if (v.Contains("GB"))
            {
                size *= 1024;
            }

            return size;
        }

        private HttpClient GetClient()
        {
            var baseUrl = new Uri(_rutorDomain);
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
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");

            cookieContainer.Add(baseUrl, new Cookie("_ddn_intercept_2_", "db8fb83e6d89b8c30dd21125624697ce"));

            return client;
        }
    }
}