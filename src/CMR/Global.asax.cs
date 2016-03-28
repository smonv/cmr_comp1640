using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using CMR.Custom;
using CMR.Jobs;
using Hangfire;
using Newtonsoft.Json;

namespace CMR
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            HangfireBootstrapper.Instance.Start();
            RecurringJob.AddOrUpdate("NotifyReportCommendDeadline", () => NotifyReportCommentDeadline.Check(),
                Cron.Hourly);
        }

        protected void Application_End()
        {
            HangfireBootstrapper.Instance.Stop();
        }
    }
}