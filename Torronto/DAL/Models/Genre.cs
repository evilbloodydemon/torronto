using LinqToDB.Mapping;

namespace Torronto.DAL.Models
{
    [Table(Name = "genres")]
    public class Genre
    {
        [Column(Name = "id"), PrimaryKey, Identity]
        public int? ID { get; set; }

        [Column(Name = "name"), NotNull]
        public string Name { get; set; }

        [Column(Name = "site_id"), NotNull]
        public int SiteID { get; set; }
    }
}