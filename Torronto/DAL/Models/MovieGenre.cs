using LinqToDB.Mapping;

namespace Torronto.DAL.Models
{
    [Table(Name = "movies_genres")]
    public class MovieGenre
    {
        [Column(Name = "movie_id"), NotNull, PrimaryKey(1)]
        public int MovieID { get; set; }

        [Column(Name = "genre_id"), NotNull, PrimaryKey(2)]
        public int GenreID { get; set; }

        [Column(Name = "position"), NotNull]
        public int Position { get; set; }


        [Association(ThisKey = "GenreID", OtherKey = "ID", CanBeNull = false)]
        public Genre Genre { get; set; }

        [Association(ThisKey = "MovieID", OtherKey = "ID", CanBeNull = false)]
        public Movie Movie { get; set; }
    }
}