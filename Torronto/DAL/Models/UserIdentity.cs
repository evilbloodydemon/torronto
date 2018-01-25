using LinqToDB.Mapping;
using System;

namespace Torronto.DAL.Models
{
    [Table(Name = "user_identities")]
    public class UserIdentity
    {
        [Column(Name = "id"), PrimaryKey, Identity]
        public int? ID { get; set; }

        [Column(Name = "user_id"), NotNull]
        public int UserID { get; set; }

        [Column(Name = "email"), NotNull]
        public string Email { get; set; }

        [Column(Name = "auth_provider_name"), NotNull]
        public string AuthProviderName { get; set; }

        [Column(Name = "auth_provider_id"), NotNull]
        public string AuthProviderID { get; set; }

        [Column(Name = "display_name"), NotNull]
        public string DisplayName { get; set; }

        [Column(Name = "created"), NotNull]
        public DateTime Created { get; set; }

        [Association(ThisKey = "UserID", OtherKey = "ID")]
        public User User { get; set; }
    }
}