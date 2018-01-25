using LinqToDB;
using System;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using Torronto.BLL.Models;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.BLL
{
    public class UserService
    {
        public User GetByCredentials(string email, string password)
        {
            using (var db = new DbTorronto())
            {
                return db.User
                    .FirstOrDefault(x => x.Email == email && x.Password == password);
            }
        }

        public User GetByIdentity(string providerName, string providerId)
        {
            using (var db = new DbTorronto())
            {
                var identity = db.UserIdentity
                    .LoadWith(i => i.User)
                    .FirstOrDefault(i => i.AuthProviderName == providerName
                                         && i.AuthProviderID == providerId);

                return identity?.User;
            }
        }

        public User AddUser(string name, string email, string providerName, string providerId)
        {
            using (var db = new DbTorronto())
            using (db.BeginTransaction())
            {
                var user = new User
                {
                    Identifier = Guid.NewGuid(),
                    DisplayName = name,
                    AuthProviderID = string.Empty,
                    AuthProviderName = string.Empty,
                    Password = "$NoPassword$",
                    Email = email,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    FilterSizes = string.Empty
                };

                user.ID = Convert.ToInt32(
                    db.InsertWithIdentity(user)
                );

                var identity = new UserIdentity
                {
                    UserID = user.ID.GetValueOrDefault(),
                    Email = email,
                    AuthProviderID = providerId,
                    AuthProviderName = providerName,
                    DisplayName = name,
                    Created = DateTime.UtcNow,
                };

                db.Insert(identity);
                db.CommitTransaction();

                return user;
            }
        }

        public UserProfile GetProfile(int? userId)
        {
            using (var db = new DbTorronto())
            {
                return db.User
                    .Where(u => u.ID == userId)
                    .Select(u => new UserProfile
                    {
                        ID = u.ID,
                        DisplayName = u.DisplayName,
                        Email = u.Email,
                        FilterAudio = u.FilterAudio,
                        FilterTraslation = u.FilterTraslation,
                        FilterVideo = u.FilterVideo,
                        FilterSizes = u.FilterSizes,
                        Identities = db.UserIdentity
                            .Where(i => i.UserID == u.ID)
                            .Select(i => new UserProfile.Identity
                            {
                                Email = i.Email,
                                AuthProviderName = i.AuthProviderName,
                                AuthProviderID = i.AuthProviderID,
                                DisplayName = i.DisplayName
                            }),
                        RssHash = Sql2.MD5(Sql.ConvertTo<string>.From(u.Identifier))
                    })
                    .FirstOrDefault();
            }
        }

        public void UpdateProfile(int? userId, UserProfile profile)
        {
            using (var db = new DbTorronto())
            {
                db.User
                    .Where(u => u.ID == userId)
                    .Set(f => f.FilterVideo, profile.FilterVideo)
                    .Set(f => f.FilterAudio, profile.FilterAudio)
                    .Set(f => f.FilterTraslation, profile.FilterTraslation)
                    .Set(f => f.FilterSizes, profile.FilterSizes)
                    .Update();
            }
        }

        public User AttachIdentity(int? userID, string name, string email, string providerName, string providerId)
        {
            using (var db = new DbTorronto())
            {
                var user = db.User
                    .FirstOrDefault(u => u.ID == userID);

                var identity = new UserIdentity
                {
                    UserID = user.ID.GetValueOrDefault(),
                    Email = email,
                    AuthProviderID = providerId,
                    AuthProviderName = providerName,
                    DisplayName = name,
                    Created = DateTime.UtcNow,
                };

                db.Insert(identity);

                return user;
            }
        }

        public void MergeAccounts(int sourceId, int destinationId)
        {
            using (var db = new DbTorronto())
            using (var trans = db.BeginTransaction())
            {
                db.UserIdentity
                    .Where(x => x.UserID == sourceId)
                    .Set(f => f.UserID, destinationId)
                    .Update();

                // transfer torrents

                var sourceTorrents = db.TorrentUser
                    .Where(x => x.UserID == sourceId)
                    .Select(x => x.TorrentID)
                    .ToList();

                var destTorrents = db.TorrentUser
                    .Where(x => x.UserID == destinationId)
                    .Select(x => x.TorrentID)
                    .ToList();

                var updateTorrents = sourceTorrents
                    .Except(destTorrents);

                db.TorrentUser
                    .Where(x => updateTorrents.Contains(x.TorrentID) && x.UserID == sourceId)
                    .Set(f => f.UserID, destinationId)
                    .Update();

                // transfer movies

                var sourceMovies = db.MovieUser
                    .Where(x => x.UserID == sourceId)
                    .Select(x => x.MovieID)
                    .ToList();

                var destMovies = db.MovieUser
                    .Where(x => x.UserID == destinationId)
                    .Select(x => x.MovieID)
                    .ToList();

                var updateMovies = sourceMovies
                    .Except(destMovies);

                db.MovieUser
                    .Where(x => updateMovies.Contains(x.MovieID) && x.UserID == sourceId)
                    .Set(f => f.UserID, destinationId)
                    .Update();

                //remove all source items

                db.TorrentUser
                    .Where(x => x.UserID == sourceId)
                    .Delete();

                db.MovieUser
                    .Where(x => x.UserID == sourceId)
                    .Delete();

                db.User
                    .Where(x => x.ID == sourceId)
                    .Delete();

                trans.Commit();
            }
        }
    }
}