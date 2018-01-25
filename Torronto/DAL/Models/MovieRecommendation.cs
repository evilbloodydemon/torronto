using LinqToDB.Mapping;

namespace Torronto.DAL.Models
{
    [Table(Name = "movies_recommendations")]
    public class MovieRecommendation
    {
        [Column(Name = "movie_id"), NotNull, PrimaryKey(1)]
        public int MovieID { get; set; }

        [Column(Name = "other_movie_id"), NotNull, PrimaryKey(2)]
        public int OtherMovieID { get; set; }

        [Column(Name = "position"), NotNull]
        public int Position { get; set; }
    }
}