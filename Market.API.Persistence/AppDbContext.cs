using Market.API.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Market.API.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);   // important: sets up Identity tables first
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("tbl_Users", schema: "core");
            });

            builder.Entity<ApplicationRole>(b =>
            {
                b.ToTable("tbl_Roles", schema: "core");
            });

            builder.Entity<IdentityUserRole<Guid>>(b =>
            {
                b.ToTable("tbl_UserRoles", schema: "core");
            });

            builder.Entity<IdentityUserClaim<Guid>>(b =>
            {
                b.ToTable("tbl_UserClaims", schema: "core");
            });

            builder.Entity<IdentityUserLogin<Guid>>(b =>
            {
                b.ToTable("tbl_UserLogins", schema: "core");
            });

            builder.Entity<IdentityRoleClaim<Guid>>(b =>
            {
                b.ToTable("tbl_RoleClaims", schema: "core");
            });

            builder.Entity<IdentityUserToken<Guid>>(b =>
            {
                b.ToTable("tbl_UserTokens", schema: "core");
            });
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
