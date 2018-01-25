using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using Torronto.DAL.Models;

namespace Torronto.DAL
{
    public class DbTorronto : DataConnection
    {
        public DbTorronto()
            : base("torronto")
        {
        }

        public DbTorronto(string configString)
            : base(configString)
        {
        }

        public ITable<Torrent> Torrent => GetTable<Torrent>();

        public ITable<Movie> Movie => GetTable<Movie>();

        public ITable<MovieUser> MovieUser => GetTable<MovieUser>();

        public ITable<User> User => GetTable<User>();

        public ITable<UserIdentity> UserIdentity => GetTable<UserIdentity>();

        public ITable<TorrentUser> TorrentUser => GetTable<TorrentUser>();

        public ITable<Person> Person => GetTable<Person>();

        public ITable<MoviePerson> MoviePerson => GetTable<MoviePerson>();

        public ITable<Genre> Genre => GetTable<Genre>();

        public ITable<MovieGenre> MovieGenre => GetTable<MovieGenre>();

        public ITable<MovieRecommendation> MovieRecommendation => GetTable<MovieRecommendation>();
    }

    public static class DbTorrontoExt
    {
        public static IQueryable<T> NoCopyrighted<T>(this IQueryable<T> table) where T : ICopyrighted
        {
            return table.Where(x => x.IsCopyrighted == false);
        }
    }
}