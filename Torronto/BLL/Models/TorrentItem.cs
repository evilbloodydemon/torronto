using Torronto.DAL.Models;

namespace Torronto.BLL.Models
{
    public class TorrentItem
    {
        public Torrent Self { get; set; }
        public bool InWaitList { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsRss { get; set; }
        public bool IsCopyrighted { get; set; }
    }
}