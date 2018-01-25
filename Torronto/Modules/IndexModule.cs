using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Nancy;
using NLog;
using SquishIt.Framework;
using Torronto.BLL;
using Torronto.BLL.Ext;
using Torronto.BLL.Models;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.Modules
{
    public class IndexModule : NancyModule
    {
        private readonly TorrentService _torrentService;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public IndexModule(
            SearchService searchService,
            TorrentService torrentService
        )
        {
            _torrentService = torrentService;

            Get["/"] = parameters =>
            {
                var user = this.GetUser() ?? new User();

                var model = new
                {
                    User = user,
                    UserPresent = user.ID != null,
                    FilterVideo = (int)user.FilterVideo,
                    FilterAudio = (int)user.FilterAudio,
                    FilterTraslation = (int)user.FilterTraslation,
                    FilterSizes = user.FilterSizes,

                    LibScript = Bundle.JavaScript().RenderCachedAssetTag("lib"),
                    AppScript = Bundle.JavaScript().RenderCachedAssetTag("app"),
                    LibCss = Bundle.Css().RenderCachedAssetTag("css")
                };

                return View["index", model];
            };

            Get["/test"] = parameters =>
            {
                using (var db = new DbTorronto())
                {
                    var user = db.User.FirstOrDefault(x => x.ID == 3);
                    var message = new StringBuilder()
                        .AppendLine("оПХБЕР");

                    EmailService.SendMail(user, "test", message);
                }
                return 200;
            };

            Get["/rss/{userHash}"] = GetUserRss;
        }


        private dynamic GetUserRss(dynamic parameters)
        {
            var userHash = (string)parameters.userHash;
            var items = _torrentService.GetPersonalFeed(userHash);

            return Response.AsText(GenerateRss(items), "text/xml");
        }

        private string GenerateRss(IEnumerable<RssItem> items)
        {
            var xChannel = new XElement("channel",
                new XElement("title", "[Torronto] Персональная лента"),
                new XElement("description"),
                new XElement("lastBuildDate", items.Select(x => x.PubDate).DefaultIfEmpty().Max()),
                new XElement("link", "http://torronto.evilbloodydemon.ru/"));

            var xItems = items
                .Select(item => new XElement("item",
                    new XElement("title", item.Title),
                    new XElement("link", item.Link),
                    new XElement("description", item.Description),
                    new XElement("pubDate", item.PubDate)));

            xChannel.Add(xItems);

            var xRss = new XElement("rss", xChannel);
            xRss.SetAttributeValue("version", "2.0");

            var xDoc = new XDocument(xRss);

            return xDoc.ToString();
        }
    }
}