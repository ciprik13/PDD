using Microsoft.AspNetCore.Identity;

namespace AgriCure.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
