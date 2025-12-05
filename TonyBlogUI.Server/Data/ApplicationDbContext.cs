using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TonyBlogUI.Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Seed Roles
        var adminRoleId = "1";
        var userRoleId = "2";

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN"
            },
            new IdentityRole
            {
                Id = userRoleId,
                Name = "User",
                NormalizedName = "USER"
            }
        );

        // Seed Users
        var hasher = new PasswordHasher<ApplicationUser>();

        var adminUserId = "1";
        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            UserName = "admin@example.com",
            NormalizedUserName = "ADMIN@EXAMPLE.COM",
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, "testUser123!");

        var normalUserId = "2";
        var normalUser = new ApplicationUser
        {
            Id = normalUserId,
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            EmailConfirmed = true,
            FirstName = "Normal",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        normalUser.PasswordHash = hasher.HashPassword(normalUser, "testUser123!");

        builder.Entity<ApplicationUser>().HasData(adminUser, normalUser);

        // Assign Roles to Users
        builder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminUserId
            },
            new IdentityUserRole<string>
            {
                RoleId = userRoleId,
                UserId = normalUserId
            }
        );
    }
}
