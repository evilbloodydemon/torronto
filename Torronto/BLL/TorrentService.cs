using LinqToDB;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Torronto.BLL.Ext;
using Torronto.BLL.Models;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.BLL
{
    public class TorrentService
    {
        private static readonly bool _isSphinxEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["Search.IsSphinxEnabled"]);
        private readonly SearchService _searchService;
        private readonly EmailService _emailService;

        public static readonly Tuple<int, int>[] TorrentSizes =
        {
            new Tuple<int, int>(0, 1024),
            new Tuple<int, int>(1 * 1024 + 1, 3 * 1024),
            new Tuple<int, int>(3 * 1024 + 1, 5 * 1024),
            new Tuple<int, int>(5 * 1024 + 1, 15 * 1024),
            new Tuple<int, int>(15 * 1024 + 1, 1024 * 1024)
        };

        private class TorrentJoin
        {
            public Torrent Torrent { get; set; }

            public bool MuWaitlist { get; set; }

            public bool IsSubscribed { get; set; }

            public DateTime? AddedRss { get; set; }

            public bool IsCopyrighted { get; set; }
        }

        public TorrentService(
            SearchService searchService,
            EmailService emailService
        )
        {
            _searchService = searchService;
            _emailService = emailService;
        }

        public Pagination<TorrentItem> GetTorrents(int? userId, TorrentSearchParams search, PaginationParams pageParams)
        {
            using (var db = new DbTorronto())
            {
                var filter = from t in db.Torrent
                             from muWaitList in db.MovieUser.Where(mu => mu.MovieID == t.MovieID && mu.UserID == userId).DefaultIfEmpty()
                             from subscription in db.TorrentUser.Where(tu => tu.TorrentID == t.ID && tu.UserID == userId).DefaultIfEmpty()
                             from movie in db.Movie.Where(mv => mv.ID == t.MovieID).DefaultIfEmpty()
                             select new TorrentJoin
                             {
                                 Torrent = t,
                                 MuWaitlist = muWaitList.IsWaitlist,
                                 IsSubscribed = subscription.IsSubscribed,
                                 AddedRss = subscription.AddedRss,
                                 IsCopyrighted = movie.IsCopyrighted
                             };

                if (search.MovieID > 0)
                {
                    filter = filter.Where(x => x.Torrent.MovieID == search.MovieID);
                }

                if (search.WaitList)
                {
                    filter = filter.Where(x => x.MuWaitlist);
                }

                if (search.Subscription)
                {
                    filter = filter.Where(x => x.IsSubscribed || x.AddedRss != null);
                }

                if (!string.IsNullOrEmpty(search.Search))
                {
                    if (_isSphinxEnabled)
                    {
                        var torrentIds = _searchService.SearchTorrentIds(search);
                        filter = filter.Where(x => torrentIds.Contains(x.Torrent.ID));
                    }
                    else
                    {
                        filter = filter.Where(x => x.Torrent.Title.Contains(search.Search));
                    }
                }

                if (search.VideoQuality != VideoQuality.Unknown)
                {
                    var predicate = PredicateBuilder.False<TorrentJoin>();

                    predicate = Enum.GetValues(typeof(VideoQuality))
                        .Cast<VideoQuality>()
                        .Where(vq => vq != VideoQuality.Unknown)
                        .Where(vq => search.VideoQuality.HasFlag(vq))
                        .Aggregate(predicate, (current, vq) => current.Or(x => x.Torrent.VideoQuality == vq));

                    filter = filter.Where(predicate);
                }

                if (search.AudioQuality != AudioQuality.Unknown)
                {
                    var predicate = PredicateBuilder.False<TorrentJoin>();

                    predicate = Enum.GetValues(typeof(AudioQuality))
                        .Cast<AudioQuality>()
                        .Where(aq => aq != AudioQuality.Unknown)
                        .Where(aq => search.AudioQuality.HasFlag(aq))
                        .Aggregate(predicate, (current, aq) => current.Or(x => x.Torrent.AudioQuality == aq));

                    filter = filter.Where(predicate);
                }

                if (search.TranslationQuality != Translation.Unknown)
                {
                    var predicate = PredicateBuilder.False<TorrentJoin>();

                    predicate = Enum.GetValues(typeof(Translation))
                        .Cast<Translation>()
                        .Where(translation => translation != Translation.Unknown)
                        .Where(translation => search.TranslationQuality.HasFlag(translation))
                        .Aggregate(predicate, (current, translation) => current.Or(x => x.Torrent.Translation == translation));

                    filter = filter.Where(predicate);
                }

                if (search.TorrentCategory != TorrentCategory.Unknown)
                {
                    var predicate = PredicateBuilder.False<TorrentJoin>();

                    predicate = Enum.GetValues(typeof(TorrentCategory))
                        .Cast<TorrentCategory>()
                        .Where(category => category != TorrentCategory.Unknown)
                        .Where(category => search.TorrentCategory.HasFlag(category))
                        .Aggregate(predicate, (current, category) => current.Or(x => x.Torrent.Category == category));

                    filter = filter.Where(predicate);
                }

                if (!string.IsNullOrEmpty(search.Sizes))
                {
                    var index = 0;
                    var nums = search.Sizes
                        .Split(',')
                        .Select(x => Convert.ToInt32(x))
                        .ToArray();

                    var predicate = PredicateBuilder.False<TorrentJoin>();

                    foreach (var tuple in TorrentSizes)
                    {
                        if (nums.Contains(index))
                        {
                            predicate = predicate.Or(x => x.Torrent.Size >= tuple.Item1 && x.Torrent.Size <= tuple.Item2);
                        }
                        index++;
                    }

                    filter = filter.Where(predicate);
                }

                switch (search.Order)
                {
                    case "size":
                        filter = filter
                            .OrderByDescending(x => x.Torrent.VideoQuality)
                            .ThenByDescending(x => x.Torrent.Size);
                        break;

                    default:
                        filter = filter
                            .OrderByDescending(x => x.Torrent.Created)
                            .ThenByDescending(x => x.Torrent.ID);
                        break;
                }

                var count = pageParams.NoCount ? 0 : filter.Count();
                var items = filter
                    .Skip(pageParams.SkipCount)
                    .Take(pageParams.PageSize)
                    .Select(x => new TorrentItem
                    {
                        Self = x.Torrent,
                        InWaitList = x.MuWaitlist,
                        IsSubscribed = x.IsSubscribed,
                        IsRss = x.AddedRss != null,
                        IsCopyrighted = x.IsCopyrighted
                    })
                    .ToList();

                items.RemoveAll(t => t.IsCopyrighted);

                return new Pagination<TorrentItem>(items)
                {
                    TotalItems = count,
                    PageSize = pageParams.PageSize
                };
            }
        }

        public TorrentItem GetSingleTorrent(int torrentID, int? userId)
        {
            using (var db = new DbTorronto())
            {
                var item = db.Torrent
                    .LoadWith(t => t.Movie)
                    .FirstOrDefault(torrent => torrent.ID == torrentID && Sql2.IsNullOrFalse(torrent.Movie.IsCopyrighted));

                var subscription = db.TorrentUser
                    .FirstOrDefault(tu => tu.TorrentID == torrentID && tu.UserID == userId);

                return new TorrentItem
                {
                    Self = item,
                    IsSubscribed = subscription?.IsSubscribed == true,
                    IsRss = subscription?.AddedRss != null
                };
            }
        }

        public Torrent GetById(int torrentId)
        {
            using (var db = new DbTorronto())
            {
                return db.Torrent
                    .FirstOrDefault(m => m.ID == torrentId);
            }
        }

        public void SaveToDb(List<Torrent> torrents)
        {
            using (var db = new DbTorronto())
            {
                foreach (var torrent in torrents)
                {
                    var existing = db.Torrent
                        .FirstOrDefault(x => x.SiteID == torrent.SiteID);

                    if (existing == null)
                    {
                        torrent.Updated = DateTime.UtcNow;
                        db.Insert(torrent);
                    }
                    else
                    {
                        db.Torrent
                            .Where(x => x.ID == existing.ID)
                            .Set(f => f.Title, torrent.Title)
                            .Set(f => f.InfoHash, torrent.InfoHash)
                            .Set(f => f.Size, torrent.Size)
                            .Set(f => f.Updated, DateTime.UtcNow)
                            .Update();

                        if (existing.Size != torrent.Size)
                        {
                            var subscribers = from tu in db.TorrentUser
                                              join u in db.User on tu.UserID equals u.ID
                                              where tu.IsSubscribed && tu.TorrentID == existing.ID
                                              select u;

                            existing.Title = torrent.Title;

                            foreach (var subscriber in subscribers)
                            {
                                _emailService.NotifyUserAboutTorrent(subscriber, existing);
                            }
                        }
                    }
                }
            }
        }

        public void SaveDetails(Torrent torrent, bool isRemoved)
        {
            using (var db = new DbTorronto())
            {
                var filter = db.Torrent
                    .Where(x => x.ID == torrent.ID)
                    .Set(f => f.Updated, DateTime.UtcNow)
                    .Set(f => f.IsDetailed, true);

                if (isRemoved)
                {
                    filter = filter.Set(f => f.IsRemoved, true);
                }
                else
                {
                    filter = filter
                        .Set(f => f.ImdbID, torrent.ImdbID)
                        .Set(f => f.KinopoiskID, torrent.KinopoiskID)
                        .Set(f => f.VideoQuality, torrent.VideoQuality)
                        .Set(f => f.AudioQuality, torrent.AudioQuality)
                        .Set(f => f.Translation, torrent.Translation)
                        .Set(f => f.IsRemoved, false);
                }

                filter.Update();
            }
        }

        public void AddTorrentUserLink(int torrentID, int? userId, bool subscribe, bool rss)
        {
            if (userId == null) return;

            using (var db = new DbTorronto())
            {
                var update = db.TorrentUser
                    .Where(tu => tu.UserID == userId && tu.TorrentID == torrentID)
                    .AsUpdatable();

                if (subscribe)
                {
                    update = update.Set(f => f.IsSubscribed, true);
                }

                if (rss)
                {
                    update = update.Set(f => f.AddedRss, DateTime.UtcNow);
                }

                var affected = update.Update();

                if (affected == 0)
                {
                    var newTu = new TorrentUser
                    {
                        EmailSent = null,
                        UserID = userId.GetValueOrDefault(),
                        TorrentID = torrentID
                    };

                    if (subscribe) newTu.IsSubscribed = true;
                    if (rss) newTu.AddedRss = DateTime.UtcNow;

                    db.Insert(newTu);
                }
            }
        }

        public void RemoveTorrentUserLink(int torrentID, int? userId, bool subscribe, bool rss)
        {
            using (var db = new DbTorronto())
            {
                var update = db.TorrentUser
                    .Where(tu => tu.UserID == userId && tu.TorrentID == torrentID)
                    .Set(f => f.TorrentID, torrentID);

                if (subscribe)
                {
                    update = update.Set(f => f.IsSubscribed, false);
                }

                if (rss)
                {
                    update = update.Set(f => f.AddedRss, (DateTime?)null);
                }

                update.Update();
            }
        }

        public IEnumerable<RssItem> GetPersonalFeed(string userHash)
        {
            using (var db = new DbTorronto())
            {
                var user = db.User
                    .FirstOrDefault(u => Sql.Like(Sql2.MD5(Sql.ConvertTo<string>.From(u.Identifier)), userHash));

                if (user == null) return Enumerable.Empty<RssItem>();

                var filter = db.TorrentUser
                    .Where(tu => tu.UserID == user.ID && tu.AddedRss != null && Sql2.IsNullOrFalse(tu.Torrent.Movie.IsCopyrighted));

                var orderedByAdded = filter
                    .OrderByDescending(x => x.AddedRss)
                    .Select(x => new RssItem
                    {
                        Title = TorrentTitle(x.Torrent.Movie.Title, x.Torrent.Title),
                        Link = TorrentUrl(x.Torrent.InfoHash),
                        Description = x.Torrent.Movie.Description,
                        PubDate = x.AddedRss.GetValueOrDefault()
                    })
                    .Take(20)
                    .ToList();

                var orderedByUpdated = filter
                    .OrderByDescending(x => x.Torrent.Updated)
                    .Select(x => new RssItem
                    {
                        Title = TorrentTitle(x.Torrent.Movie.Title, x.Torrent.Title),
                        Link = TorrentUrl(x.Torrent.InfoHash),
                        Description = x.Torrent.Movie.Description,
                        PubDate = x.Torrent.Updated
                    })
                    .Take(20)
                    .ToList();

                var items = orderedByAdded
                    .Union(orderedByUpdated)
                    .GroupBy(x => x.Link)
                    .Select(g => g.OrderByDescending(x => x.PubDate).First())
                    .OrderByDescending(x => x.PubDate)
                    .Take(20)
                    .ToList();

                return items;
            }
        }

        private string TorrentTitle(string movieTitle, string torrentTitle)
        {
            return !string.IsNullOrEmpty(movieTitle) ? movieTitle : torrentTitle;
        }

        private static string TorrentUrl(string infoHash)
        {
            return $"magnet:?xt=urn:btih:{infoHash}&tr=udp://bt.top-tor.org:2710&tr=http://retracker.local/announce";
        }
    }
}