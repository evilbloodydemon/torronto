using LinqToDB.Mapping;
using System;

namespace Torronto.DAL.Models
{
    [Table(Name = "torrents")]
    public class Torrent
    {
        [Column(Name = "id"), PrimaryKey, Identity]
        public int? ID { get; set; }

        [Column(Name = "title"), NotNull]
        public string Title { get; set; }

        [Column(Name = "site_id"), NotNull]
        public int SiteID { get; set; }

        [Column(Name = "kinopoisk_id"), Nullable]
        public int? KinopoiskID { get; set; }

        [Column(Name = "imdb_id"), Nullable]
        public int? ImdbID { get; set; }

        [Column(Name = "size"), NotNull]
        public decimal Size { get; set; }

        [Column(Name = "is_detailed"), NotNull]
        public bool IsDetailed { get; set; }

        [Column(Name = "is_removed"), NotNull]
        public bool IsRemoved { get; set; }

        [Column(Name = "info_hash"), NotNull]
        public string InfoHash { get; set; }

        [Column(Name = "created"), NotNull]
        public DateTime Created { get; set; }

        [Column(Name = "updated"), NotNull]
        public DateTime Updated { get; set; }

        [Column(Name = "movie_id"), Nullable]
        public int? MovieID { get; set; }

        [Column(Name = "file_type"), NotNull]
        public FileType FileType { get; set; }

        [Column(Name = "video_quality"), NotNull]
        public VideoQuality VideoQuality { get; set; }

        [Column(Name = "sound_quality"), NotNull]
        public AudioQuality AudioQuality { get; set; }

        [Column(Name = "translation"), NotNull]
        public Translation Translation { get; set; }

        [Column(Name = "category"), NotNull]
        public TorrentCategory Category { get; set; }

        [Association(ThisKey = "MovieID", OtherKey = "ID")]
        public Movie Movie { get; set; }
    }

    public enum FileType
    {
        Unknown = 0,
        Mpeg4 = 1,
        X264 = 2,
        Dvd = 3,
        BluRay = 4
    }

    [Flags]
    public enum VideoQuality
    {
        Unknown = 0,
        Bad = 1,
        Mediocre = 2,
        Good = 4, //20,
        HighDefinition = 8,
    }

    [Flags]
    public enum AudioQuality
    {
        Unknown = 0,
        Bad = 1,
        Good = 4,
        HighDefinition = 8,
    }

    [Flags]
    public enum Translation
    {
        Unknown = 0,
        Subtitles = 1,
        VoiceOver = 4,
        Dub = 8,
    }

    [Flags]
    public enum TorrentCategory
    {
        Unknown = 0,
        Foreign = 1,
        Russian = 2,
        TvSeries = 4,
        Cartoons = 8
    }
}