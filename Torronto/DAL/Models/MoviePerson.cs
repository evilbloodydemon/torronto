using LinqToDB.Mapping;

namespace Torronto.DAL.Models
{
    [Table(Name = "movies_persons")]
    public class MoviePerson
    {
        [Column(Name = "movie_id"), NotNull, PrimaryKey(1)]
        public int MovieID { get; set; }

        [Column(Name = "person_id"), NotNull, PrimaryKey(2)]
        public int PersonID { get; set; }

        [Column(Name = "position"), NotNull]
        public int Position { get; set; }


        [Association(ThisKey = "PersonID", OtherKey = "ID", CanBeNull = false)]
        public Person Person { get; set; }

        [Association(ThisKey = "MovieID", OtherKey = "ID", CanBeNull = false)]
        public Movie Movie { get; set; }
    }
}