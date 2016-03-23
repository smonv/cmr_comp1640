using System.Web.Hosting;
using Hangfire;

namespace CMR.Helpers
{
    public class HangfireBootstrapper : IRegisteredObject
    {
        public static readonly HangfireBootstrapper Instance = new HangfireBootstrapper();

        private readonly object _lockObject = new object();

        private BackgroundJobServer _backgroundJobServer;
        private bool _started;

        private HangfireBootstrapper()
        {
        }

        void IRegisteredObject.Stop(bool immediate)
        {
            Stop();
        }

        public void Start()
        {
            lock (_lockObject)
            {
                if (_started) return;
                _started = true;

                HostingEnvironment.RegisterObject(this);

                GlobalConfiguration.Configuration
                    .UseSqlServerStorage("DefaultConnection");

                _backgroundJobServer = new BackgroundJobServer();
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                if (_backgroundJobServer != null)
                {
                    _backgroundJobServer.Dispose();
                }

                HostingEnvironment.UnregisterObject(this);
            }
        }
    }
}