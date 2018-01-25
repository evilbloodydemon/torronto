using Nancy;
using System;
using System.Linq;
using Torronto.BLL;

namespace Torronto.Modules
{
    public class SearchModule : NancyModule
    {
        private readonly SearchService _searchService;

        public SearchModule(SearchService searchService)
            : base("/api/search")
        {
            _searchService = searchService;
            Get["/movie_completion"] = MovieCompletion;
        }

        private dynamic MovieCompletion(dynamic parameters)
        {
            var title = (string)Request.Query.title;

            return Response.AsJson(new
            {
                results = _searchService.MovieCompletion(title)
                    .Select(t => new
                    {
                        ID = t.ID,
                        Title = t.Title
                    })
            });
        }
    }
}