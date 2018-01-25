using Nancy.TinyIoc;
using NLog;
using Quartz;
using System;
using Torronto.BLL;

namespace Torronto.Jobs
{
    [DisallowConcurrentExecution]
    internal class RutorJob : IInterruptableJob
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly RutorService _rutorService;

        public RutorJob()
        {
            var container = TinyIoCContainer.Current;
            _rutorService = container.Resolve<RutorService>();
        }

        public void Execute(IJobExecutionContext context)
        {
            _logger.Info("doing rutorjob");
            try
            {
                _rutorService.ParseFeed();
            }
            catch (Exception ex)
            {
                _logger.Warn("RUTOR", ex);
            }
        }

        public void Interrupt()
        {
        }
    }
}