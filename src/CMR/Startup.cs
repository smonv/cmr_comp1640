using CMR;
using CMR.Custom;
using Hangfire;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace CMR
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                AuthorizationFilters = new[] { new HangfireAuthorizationFilter() }
            });
        }
    }
}