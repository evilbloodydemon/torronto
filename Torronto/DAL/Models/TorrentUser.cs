using LinqToDB.Mapping;
using System;

namespace Torronto.DAL.Models
{
    [Table(Name = "torrents_users")]
    public class TorrentUser
    {
        [Column(Name = "torrent_id"), NotNull, PrimaryKey(1)]
        public int TorrentID { get; set; }

        [Column(Name = "user_id"), NotNull, PrimaryKey(2)]
        public int UserID { get; set; }

        [Column(Name = "email_sent"), Nullable]
        public DateTime? EmailSent { get; set; }

        [Column(Name = "is_subscribed")]
        public bool IsSubscribed { get; set; }

        [Column(Name = "added_rss")]
        public DateTime? AddedRss { get; set; }

        [Association(ThisKey = "TorrentID", OtherKey = "ID", CanBeNull = false)]
        public Torrent Torrent { get; set; }
    }
}