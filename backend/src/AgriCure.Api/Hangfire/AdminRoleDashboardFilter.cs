using AgriCure.Infrastructure.Identity;
using Hangfire.Dashboard;

namespace AgriCure.Api.Hangfire;

/// <summary>
/// Restricts the Hangfire dashboard to authenticated users in the <c>admin</c> role.
/// JWT bearer authentication runs before the dashboard middleware, so the principal
/// is already populated by the time <see cref="Authorize"/> is invoked.
/// </summary>
internal sealed class AdminRoleDashboardFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) =>
        context.GetHttpContext().User.IsInRole(ApplicationRole.Admin);
}
