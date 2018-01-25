using LinqToDB.Mapping;
using System;

namespace Torronto.DAL.Models
{
    [Table(Name = "users")]
    public class User
    {
        [Column(Name = "id"), PrimaryKey, Identity]
        public int? ID { get; set; }

        [Column(Name = "email"), NotNull]
        public string Email { get; set; }

        [Column(Name = "password"), NotNull]
        public string Password { get; set; }

        [Column(Name = "identifier"), NotNull]
        public Guid Identifier { get; set; }

        [Column(Name = "auth_provider_name"), NotNull]
        public string AuthProviderName { get; set; }

        [Column(Name = "auth_provider_id"), NotNull]
        public string AuthProviderID { get; set; }

        [Column(Name = "display_name"), NotNull]
        public string DisplayName { get; set; }

        [Column(Name = "created"), NotNull]
        public DateTime Created { get; set; }

        [Column(Name = "updated"), NotNull]
        public DateTime Updated { get; set; }

        [Column(Name = "filter_video"), NotNull]
        public VideoQuality FilterVideo { get; set; }

        [Column(Name = "filter_audio"), NotNull]
        public AudioQuality FilterAudio { get; set; }

        [Column(Name = "filter_translation"), NotNull]
        public Translation FilterTraslation { get; set; }

        [Column(Name = "filter_sizes"), NotNull]
        public string FilterSizes { get; set; }
    }
}