using System.Collections.Generic;
using Hangfire.Dashboard;
using Microsoft.Owin;

namespace CMR.Custom
{
    public class HangfireAuthorizationFilter : IAuthorizationFilter
    {
        public bool Authorize(IDictionary<string, object> owinEnvironment)
        {
            var context = new OwinContext(owinEnvironment);
            var user = context.Authentication.User;
            return user.Identity.IsAuthenticated && user.IsInRole("Administrator");
        }
    }
}