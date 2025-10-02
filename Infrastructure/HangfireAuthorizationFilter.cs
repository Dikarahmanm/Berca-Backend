using Hangfire.Dashboard;

namespace Berca_Backend.Infrastructure
{
    /// <summary>
    /// Authorization filter for Hangfire dashboard
    /// Only allows Admin users to access the dashboard
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // In development, allow all access
            if (httpContext.Request.Host.Host == "localhost")
            {
                return true;
            }

            // In production, check if user is authenticated and is Admin
            return httpContext.User.Identity?.IsAuthenticated == true &&
                   httpContext.User.IsInRole("Admin");
        }
    }
}
