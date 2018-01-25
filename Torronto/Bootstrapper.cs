using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Cryptography;
using Nancy.TinyIoc;
using NLog;
using SquishIt.Framework;
using System;
using System.Configuration;
using Torronto.BLL;

namespace Torronto
{
    using Nancy;

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly int _overridePort = Convert.ToInt32(ConfigurationManager.AppSettings["xsa.OverridePort"]);
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static readonly CryptographyConfiguration _cryptographyConfiguration = new CryptographyConfiguration(
            new RijndaelEncryptionProvider(new PassphraseKeyGenerator("a4c1cf1f5fe8be2a42b6906c4bfa48a1", new byte[] { 1, 32, 95, 226, 57, 58, 33, 41, })),
            new DefaultHmacProvider(new PassphraseKeyGenerator("69d38569c0a5f0f48f94f15cf83c7292", new byte[] { 11, 21, 43, 54, 53, 64, 227, 81 }))
            );

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            var formsAuthConfig = new FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/login",
                UserMapper = container.Resolve<IUserMapper>(),
                CryptographyConfiguration = _cryptographyConfiguration
            };

            FormsAuthentication.Enable(pipelines, formsAuthConfig);

            pipelines.BeforeRequest += (ctx) =>
            {
                if (_overridePort > 0)
                {
                    ctx.Request.Url.Port = _overridePort;
                }

                return null;
            };

            pipelines.OnError += (ctx, ex) =>
            {
                _logger.Warn("APP", ex);
                _logger.Warn("APPi", ex.InnerException);

                return null;
            };
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            SetupBundles();
        }

        private void SetupBundles()
        {
            Bundle.JavaScript()
                .AddMinified("/Content/js/lib/jquery-2.1.1.min.js")
                .AddMinified("/Content/js/lib/lodash.min.js")
                .AddMinified("/Content/js/lib/angular.min.js")
                .AddMinified("/Content/js/lib/angular-route.min.js")
                .AddMinified("/Content/js/lib/angular-resource.min.js")
                .AddMinified("/Content/js/lib/angular-sanitize.min.js")
                .AddMinified("/Content/js/lib/angulartics.js")
                .AddMinified("/Content/js/lib/angulartics-ga.js")
                .AddMinified("/Content/js/lib/ui-bootstrap-tpls-0.11.0.min.js")
                .ForceRelease()
                .AsNamed("lib", "/Content/cached/lib_#.js");

            Bundle.JavaScript()
                .AddMinified("/Content/js/app/controllers.js")
                .AddMinified("/Content/js/app/controllers/_common.js")
                .AddMinified("/Content/js/app/controllers/dashboard.js")
                .AddMinified("/Content/js/app/controllers/dashboard_new_movies.js")
                .AddMinified("/Content/js/app/controllers/dashboard_top_week.js")
                .AddMinified("/Content/js/app/controllers/dashboard_recommended.js")
                .AddMinified("/Content/js/app/controllers/dashboard_waitlist.js")
                .AddMinified("/Content/js/app/controllers/dashboard_torrents.js")
                .AddMinified("/Content/js/app/controllers/dashboard_search.js")
                .AddMinified("/Content/js/app/controllers/profile.js")
                .AddMinified("/Content/js/app/services.js")
                .AddMinified("/Content/js/app/filters.js")
                .AddMinified("/Content/js/app/directives.js")
                .AddMinified("/Content/js/app/app.js")
                .AddMinified("/Content/js/app/angucomplete.js")
                .ForceRelease()
                .AsNamed("app", "/Content/cached/app_#.js");

            Bundle.Css()
                .AddMinified("/Content/css/bootstrap.min.css")
                .AddMinified("/Content/css/font-awesome.css")
                .AddMinified("/Content/css/angucomplete.css")
                .AddMinified("/Content/css/torronto.css")
                .ForceRelease()
                .AsNamed("css", "/Content/cached/css_#.css");
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<SearchService>().AsSingleton();
            container.Register<KinopoiskService>().AsSingleton();
            container.Register<MovieService>().AsSingleton();
            container.Register<EmailService>().AsSingleton();
            container.Register<RutorService>().AsSingleton();
            container.Register<TorrentService>().AsSingleton();
            container.Register<UserService>().AsSingleton();
        }
    }
}