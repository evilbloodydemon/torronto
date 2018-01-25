using System.Collections.Generic;
using CsQuery;
using LinqToDB;
using LinqToDB.Data;
using Nancy;
using Nancy.TinyIoc;
using Quartz;
using Quartz.Impl;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CsQuery.Output;
using Torronto.BLL;
using Torronto.DAL;
using Torronto.DAL.Models;
using Torronto.Jobs;

namespace Torronto
{
    using Nancy.Hosting.Self;
    using System;

    internal class Torronto
    {
        private static readonly IScheduler _sheduler = (new StdSchedulerFactory()).GetScheduler();

        private static void Main(string[] args)
        {
            InitEnvironment();

            var container = TinyIoCContainer.Current;
            var queueService = container.Resolve<QueueService>();

            queueService.Start();

            if (!ProcessCommands(args))
            {
                var uri = new Uri("http://localhost:19002");

                using (var host = new NancyHost(uri))
                {
                    host.Start();

                    Console.WriteLine($"Torronto is running on {uri}");
                    Console.WriteLine("Press any [Enter] to close the host.");
                    Console.ReadLine();
                }
            }

            _sheduler.Shutdown();
            queueService.Stop();
        }

        private static void InitEnvironment()
        {
#if DEBUG
            //LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

            DataConnection.TurnTraceSwitchOn();
            DataConnection.WriteTraceLine = (s, s1) => Debug.WriteLine(s, s1);
#endif
            LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

            StaticConfiguration.DisableErrorTraces = false;

            CsQuery.Config.OutputFormatter = CsQuery.OutputFormatters.HtmlEncodingNone;
            CsQuery.Config.HtmlEncoder = CsQuery.HtmlEncoders.None;

            Nancy.Json.JsonSettings.RetainCasing = true;

            Console.WriteLine("Working dir: {0}", Environment.CurrentDirectory);
        }

        private static bool ProcessCommands(string[] args)
        {
            var commands = args.Where(x => !x.StartsWith("--")).ToArray();
            var command = commands.Take(1).FirstOrDefault();

            if (!args.Any(x => x.Equals("--no-cron", StringComparison.InvariantCultureIgnoreCase)))
            {
                InitJobs();
            }

            if (command == "misc")
            {
                foreach (var source in Enumerable.Range(1, 1000))
                {
                    //    QueueService.AddTorrentForDetails(source);
                    //    QueueService.AddMovieForRating(source);
                }
            }
            else if (command == "reindex")
            {
                var searchService = new SearchService();

                Console.WriteLine("Reindexing of torrents started");
                var torrentCount = searchService.ReindexTorrents();
                Console.WriteLine("Reindexing of torrents finished ({0} items)", torrentCount);

                Console.WriteLine("Reindexing of movies started");
                var movieCount = searchService.ReindexMovies();
                Console.WriteLine("Reindexing of movies finished ({0} items)", movieCount);

                return true;
            }
            else if (command == "identities")
            {
                SplitIdentities();

                return true;
            }
            else if (command == "pgmigrate")
            {
                PgMigrate();

                return true;
            }

            return false;
        }

        private static void PgMigrate()
        {
            Console.WriteLine("Migration started");

            using (var db = new DbTorronto())
            using (var postgres = new DbTorronto("torrontopg"))
            {
                postgres.Execute("select truncate_tables('postgres')");

                TransferItemsWithId<Movie>(db, postgres, "movies", true);
                TransferItemsWithId<Torrent>(db, postgres, "torrents", true);
                TransferItemsWithId<Genre>(db, postgres, "genres", true);
                TransferItemsWithId<Person>(db, postgres, "persons", true);
                TransferItemsWithId<User>(db, postgres, "users", true);

                TransferItemsWithId<MovieGenre>(db, postgres, "movies_genres", false);
                TransferItemsWithId<MoviePerson>(db, postgres, "movies_persons", false);
                TransferItemsWithId<MovieRecommendation>(db, postgres, "movies_recommendations", false);
                TransferItemsWithId<MovieUser>(db, postgres, "movies_users", true);
                TransferItemsWithId<TorrentUser>(db, postgres, "torrents_users", false);
                TransferItemsWithId<UserIdentity>(db, postgres, "user_identities", true);
            }

            Console.WriteLine("Migration finished");
        }

        private static void TransferItemsWithId<T>(DbTorronto db, DbTorronto postgres, string tableName, bool resetSeq) where T : class
        {
            List<T> items;
            var offset = 0;
            var count = 100;

            var options = new BulkCopyOptions
            {
                KeepIdentity = true
            };

            Console.WriteLine("TABLE {0}", tableName);
            do
            {
                items = db.GetTable<T>()
                    .Skip(offset)
                    .Take(count)
                    .ToList();

                postgres.BulkCopy(options, items);

                offset += count;

                Console.WriteLine("processed: {0}", offset);
            } while (items.Count > 0);

            if (resetSeq)
            {
                postgres.Execute(string.Format(@"
                    SELECT  
                        setval(pg_get_serial_sequence('{0}', 'id'), 
                        (SELECT MAX(id) FROM {0}));
                    ", tableName));
            }
        }

        private static void SplitIdentities()
        {
            using (var db = new DbTorronto())
            {
                var users = db.User
                    .Where(u => u.AuthProviderID.Length > 0)
                    .ToList();

                foreach (var user in users)
                {
                    var identity = new UserIdentity
                    {
                        Created = DateTime.UtcNow,
                        AuthProviderID = user.AuthProviderID,
                        AuthProviderName = user.AuthProviderName,
                        DisplayName = user.DisplayName,
                        Email = user.Email,
                        UserID = user.ID.GetValueOrDefault()
                    };

                    db.Insert(identity);
                }
            }
        }

        private static void InitJobs()
        {
            _sheduler.Start();

            var rutor = JobBuilder.Create<RutorJob>()
                .WithIdentity("rutorJob")
                .Build();

            var kinopoisk = JobBuilder.Create<KinopoiskJob>()
                .WithIdentity("kinopoiskJob")
                .Build();

            var rutorTrigger = TriggerBuilder.Create()
                .WithIdentity("rutorTrigger")
                .StartNow()
                .WithCronSchedule("23 34 * * * ?")
                .Build();

            var kinopoiskTrigger = TriggerBuilder.Create()
                .WithIdentity("kinopoiskTrigger")
                .StartNow()
                .WithCronSchedule("12 03 * * * ?")
                .Build();

            _sheduler.ScheduleJob(rutor, rutorTrigger);
            _sheduler.ScheduleJob(kinopoisk, kinopoiskTrigger);

            //_sheduler.TriggerJob(new JobKey("rutorJob"));
            //_sheduler.TriggerJob(new JobKey("kinopoiskJob"));
        }
    }
}