using System;
using Torronto.DAL.Models;

namespace Torronto.BLL.Models
{
    public class SearchParams
    {
        public string Search { get; set; }
        public bool WaitList { get; set; }

        protected T ParseToEnum<T>(string input)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), input);
            }
            catch (ArgumentException)
            {
                return default(T);
            }
        }
    }

    public class TorrentSearchParams : SearchParams
    {
        public string Sizes { get; set; }

        public string Vq
        {
            get { return string.Empty; }
            set { VideoQuality = ParseToEnum<VideoQuality>(value); }
        }

        public string Aq
        {
            get { return string.Empty; }
            set { AudioQuality = ParseToEnum<AudioQuality>(value); }
        }

        public string Tq
        {
            get { return string.Empty; }
            set { TranslationQuality = ParseToEnum<Translation>(value); }
        }

        public string Tc
        {
            get { return string.Empty; }
            set { TorrentCategory = ParseToEnum<TorrentCategory>(value); }
        }

        public VideoQuality VideoQuality { get; set; }
        public AudioQuality AudioQuality { get; set; }
        public Translation TranslationQuality { get; set; }
        public TorrentCategory TorrentCategory { get; set; }

        public int MovieID { get; set; }
        public bool Subscription { get; set; }
        public string Order { get; set; }
    }

    public class MovieSearchParams : SearchParams
    {
        public bool SystemList { get; set; }

        public string Ms
        {
            get { return string.Empty; }
            set { MovieStatus = ParseToEnum<MovieStatus>(value); }
        }

        public MovieStatus MovieStatus { get; set; }
        public string Actors { get; set; }
        public int KinopoiskID { get; set; }
    }
}