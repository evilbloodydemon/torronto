using LinqToDB.Mapping;

namespace Torronto.DAL.Models
{
    [Table(Name = "movies_top_week")]
    public class MovieTopWeek
    {
        [Column(Name = "movie_id")]
        public int MovieID { get; set; }

        [Column(Name = "torrent_count")]
        public int TorrentCount { get; set; }
    }
}