using System;
using LinqToDB;
using LinqToDB.Data;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.BLL.Ext
{
    public static class DbExtensions
    {
        public static void InsertMovieUser(this DbTorronto db, int movieId, int? userId)
        {
            db.Insert(new MovieUser
            {
                Created = DateTime.UtcNow,
                MovieID = movieId,
                UserID = userId.GetValueOrDefault()
            });
        }

        public static void RefreshTopWeekMovies(this DbTorronto db)
        {
            db.Execute("REFRESH MATERIALIZED VIEW movies_top_week;");
        }
    }
}