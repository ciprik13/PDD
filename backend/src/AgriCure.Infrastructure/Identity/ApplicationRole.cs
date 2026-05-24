using Microsoft.AspNetCore.Identity;

namespace AgriCure.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public const string Admin = "admin";
    public const string User = "user";
    public const string Agriculture = "agriculture";
}
