using Torronto.DAL.Models;

namespace Torronto.BLL.Models
{
    public class MovieItem
    {
        public Movie Self { get; set; }
        public bool InWaitList { get; set; }
        public bool IsWatched { get; set; }
        public bool IsDontWant { get; set; }
        public int? Mark { get; set; }
    }
}