using Hangfire.Dashboard;

namespace AgriCure.Api.Hangfire;

/// <summary>
/// Allows the Hangfire dashboard only when ASPNETCORE_ENVIRONMENT is Development.
/// TODO PR #4: replace with an admin-role authorization filter once Identity + JWT lands.
/// </summary>
internal sealed class DevelopmentOnlyDashboardFilter(IWebHostEnvironment environment)
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => environment.IsDevelopment();
}
