using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using System;
using Torronto.BLL;
using Torronto.BLL.Ext;
using Torronto.BLL.Models;

namespace Torronto.Modules
{
    public class TorrentsModule : NancyModule
    {
        private readonly TorrentService _torrentService;

        public TorrentsModule(
            TorrentService torrentService
            )
            : base("/api/torrents")
        {
            _torrentService = torrentService;

            Get["/"] = Index;
            Get["/{torrentID}"] = Details;
            Put["/{torrentID}"] = Update;
            Delete["/{torrentID}"] = DeleteTorrent;
        }

        private Response Index(dynamic parameters)
        {
            var userId = this.GetUserID();
            var searchParams = this.Bind<TorrentSearchParams>();
            var pageParams = this.Bind<PaginationParams>();
            pageParams.DefaultPageSize = 50;

            var items = _torrentService.GetTorrents(userId, searchParams, pageParams);

            return Response.AsJson(new
            {
                Torrents = items,
                TotalItems = items.TotalItems,
                PageSize = items.PageSize
            });
        }

        private Response Details(dynamic parameters)
        {
            var userId = this.GetUserID();
            var torrentID = (int)parameters.torrentID;

            var movie = _torrentService.GetSingleTorrent(torrentID, userId);

            return Response.AsJson(movie);
        }

        private dynamic DeleteTorrent(dynamic parameters)
        {
            this.RequiresAuthentication();

            var userId = this.GetUserID();
            var torrentID = (int)parameters.torrentID;
            var subscribe = Convert.ToBoolean((string)Request.Query.subscribe);
            var rss = Convert.ToBoolean((string)Request.Query.rss);

            if (subscribe || rss)
            {
                _torrentService.RemoveTorrentUserLink(torrentID, userId, subscribe, rss);
            }

            return HttpStatusCode.OK;
        }

        private dynamic Update(dynamic parameters)
        {
            this.RequiresAuthentication();

            var userId = this.GetUserID();
            var torrentID = (int)parameters.torrentID;
            var subscribe = Convert.ToBoolean((string)Request.Query.subscribe);
            var rss = Convert.ToBoolean((string)Request.Query.rss);

            if (subscribe || rss)
            {
                _torrentService.AddTorrentUserLink(torrentID, userId, subscribe, rss);
            }

            return HttpStatusCode.OK;
        }
    }
}