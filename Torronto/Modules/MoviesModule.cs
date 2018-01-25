using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using System;
using Torronto.BLL;
using Torronto.BLL.Ext;
using Torronto.BLL.Models;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.Modules
{
    public class MoviesModule : NancyModule
    {
        private readonly MovieService _movieService;

        public MoviesModule(
            MovieService movieService
            )
            : base("/api/movies")
        {
            _movieService = movieService;

            Get["/"] = Index;
            Get["/{movieID}"] = Details;
            Put["/{movieID}"] = Update;
            Delete["/{movieID}"] = DeleteMovie;
            Get["/secret/add/{kinopoiskID}"] = SecretAdd;
        }

        private dynamic Index(dynamic parameters)
        {
            var userId = this.GetUserID();
            var searchParams = this.Bind<MovieSearchParams>();
            var pageParams = this.Bind<PaginationParams>();
            pageParams.DefaultPageSize = 25;

            var movies = _movieService.GetMovies(userId, searchParams, pageParams);

            return Response.AsJson(new
            {
                Movies = movies,
                TotalItems = movies.TotalItems,
                PageSize = movies.PageSize,
                Actors = movies.Actors
            });
        }

        private dynamic Details(dynamic parameters)
        {
            var userId = this.GetUserID();
            var movieId = (int)parameters.movieID;

            var movie = _movieService.GetMovieSingle(userId, movieId);

            return Response.AsJson(movie);
        }

        private dynamic DeleteMovie(dynamic parameters)
        {
            this.RequiresAuthentication();

            var userId = this.GetUserID();
            var movieId = (int)parameters.movieID;
            var waitList = Convert.ToBoolean((string)Request.Query.waitlist);
            var watched = Convert.ToBoolean((string)Request.Query.watched);
            var dontwant = Convert.ToBoolean((string)Request.Query.dontwant);

            if (waitList || watched || dontwant)
            {
                _movieService.RemoveMovieUserLink(movieId, userId, waitList, watched, dontwant);
            }

            return HttpStatusCode.OK;
        }

        private dynamic Update(dynamic parameters)
        {
            this.RequiresAuthentication();

            var userId = this.GetUserID();
            var movieId = (int)parameters.movieID;
            var waitList = Convert.ToBoolean((string)Request.Query.waitlist);
            var watched = Convert.ToBoolean((string)Request.Query.watched);
            var dontwant = Convert.ToBoolean((string)Request.Query.dontwant);
            var mark = Convert.ToInt32((string)Request.Query.mark);

            if (waitList || watched || dontwant || mark > 0)
            {
                _movieService.AddMovieUserLink(movieId, userId, waitList, watched, mark, dontwant);
            }

            return HttpStatusCode.OK;
        }

        private dynamic SecretAdd(dynamic parameters)
        {
            this.RequiresAuthentication();

            var kinopoiskID = (int)parameters.kinopoiskID;

            using (var db = new DbTorronto())
            {
                var movieID = _movieService.CreateMovie(db, new Movie
                {
                    KinopoiskID = kinopoiskID,
                    Title = "---"
                });

                if (movieID > 0)
                {
                    QueueService.AddMovieForDetails(movieID);
                }
            }
            
            return HttpStatusCode.OK;
        }
    }
}