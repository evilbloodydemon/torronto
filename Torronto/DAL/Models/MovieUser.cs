using LinqToDB.Mapping;
using System;

namespace Torronto.DAL.Models
{
    [Table(Name = "movies_users")]
    public class MovieUser
    {
        [Column(Name = "id"), PrimaryKey, Identity]
        public int? ID { get; set; }

        [Column(Name = "movie_id"), NotNull]
        public int MovieID { get; set; }

        [Column(Name = "user_id"), NotNull]
        public int UserID { get; set; }

        [Column(Name = "created"), NotNull]
        public DateTime Created { get; set; }

        [Column(Name = "is_watched"), NotNull]
        public bool IsWatched { get; set; }

        [Column(Name = "is_waitlist"), NotNull]
        public bool IsWaitlist { get; set; }

        [Column(Name = "is_dont_want"), NotNull]
        public bool IsDontWant { get; set; }

        [Column(Name = "mark"), Nullable]
        public int? Mark { get; set; }
    }
}