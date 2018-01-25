using Nancy.TinyIoc;
using NLog;
using Quartz;
using System;
using Torronto.BLL;

namespace Torronto.Jobs
{
    [DisallowConcurrentExecution]
    internal class KinopoiskJob : IInterruptableJob
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly KinopoiskService _kinopoiskService;

        public KinopoiskJob()
        {
            var container = TinyIoCContainer.Current;
            _kinopoiskService = container.Resolve<KinopoiskService>();
        }

        public void Execute(IJobExecutionContext context)
        {
            _logger.Info("doing kinopoiskjob");
            try
            {
                _kinopoiskService.ParseFeed();
                _kinopoiskService.UpdateRatings();
                _kinopoiskService.UpdateRecommendations();
            }
            catch (Exception ex)
            {
                _logger.Warn("KINOPOISK", ex);
            }
        }

        public void Interrupt()
        {
        }
    }
}