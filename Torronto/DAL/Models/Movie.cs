using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Torronto.DAL.Models
{
    public enum MovieStatus
    {
        Unknown = 0,
        ComingSoon = 1,
        RecentPremiere = 2
    };

    [Table(Name = "movies")]
    public class Movie : ICopyrighted
    {
        [Column(Name = "id"), PrimaryKey, Identity]
        public int? ID { get; set; }

        [Column(Name = "title"), NotNull]
        public string Title { get; set; }

        [Column(Name = "original_title"), Nullable]
        public string OriginalTitle { get; set; }

        [Column(Name = "kinopoisk_id")]
        public int? KinopoiskID { get; set; }

        [Column(Name = "imdb_id")]
        public int? ImdbID { get; set; }

        [Column(Name = "created"), NotNull]
        public DateTime Created { get; set; }

        [Column(Name = "updated"), NotNull]
        public DateTime Updated { get; set; }

        [Column(Name = "release_date"), Nullable]
        public DateTime? ReleaseDate { get; set; }

        [Column(Name = "is_detailed"), NotNull]
        public bool IsDetailed { get; set; }

        [Column(Name = "status"), NotNull]
        public MovieStatus Status { get; set; }

        [Column(Name = "rating_kinopoisk"), NotNull]
        public decimal RatingKinopoisk { get; set; }

        [Column(Name = "rating_imdb"), NotNull]
        public decimal RatingImdb { get; set; }

        [Column(Name = "rating_last_gather"), Nullable]
        public DateTime? RatingLastGather { get; set; }

        [Column(Name = "description"), Nullable]
        public string Description { get; set; }

        [Column(Name = "best_video_quality"), Nullable]
        public VideoQuality BestVideoQuality { get; set; }

        [Column(Name = "duration_minutes")]
        public int DurationMinutes { get; set; }

        [Column(Name = "last_recommendation")]
        public DateTime? LastRecommendation { get; set; }

        [Column(Name = "is_copyrighted")]
        public bool IsCopyrighted { get; set; }

        [Association(ThisKey = "ID", OtherKey = "MovieID")]
        public MovieTopWeek MovieTopWeek { get; set; }

        //non db fields

        public List<Torrent> Torrents { get; private set; }
        public List<Person> Persons { get; private set; }
        public List<Genre> Genres { get; private set; }

        public static Movie AddRelated(Movie movie, IEnumerable<Torrent> torrents)
        {
            movie.Torrents = torrents.ToList();

            return movie;
        }

        public static Movie AddRelated(Movie movie, IEnumerable<Person> persons)
        {
            movie.Persons = persons.ToList();

            return movie;
        }

        public static Movie AddRelated(Movie movie, IEnumerable<Genre> genres)
        {
            movie.Genres = genres.ToList();

            return movie;
        }
    }
}