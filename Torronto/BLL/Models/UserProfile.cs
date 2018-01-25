using System.Collections.Generic;
using Torronto.DAL.Models;

namespace Torronto.BLL.Models
{
    public class UserProfile
    {
        public int? ID { get; set; }
        public int? MergeID { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public VideoQuality FilterVideo { get; set; }
        public AudioQuality FilterAudio { get; set; }
        public Translation FilterTraslation { get; set; }
        public string FilterSizes { get; set; }

        public IEnumerable<Identity> Identities { get; set; }
        public string RssHash { get; set; }

        public class Identity
        {
            public string Email { get; set; }
            public string AuthProviderName { get; set; }
            public string DisplayName { get; set; }
            public string AuthProviderID { get; set; }
        }
    }
}