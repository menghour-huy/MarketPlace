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

                // Bound these columns so they stay in-row (avoids LOB page reads)
                // and so SQL Server can include them in a covering index.
                b.Property(u => u.FullName).HasMaxLength(200).IsRequired();
                b.Property(u => u.SecondaryEmail).HasMaxLength(256);
                b.Property(u => u.ImageUrl).HasMaxLength(500);

                // Covering non-clustered index for GetUserProfileAsync.
                // SQL Server can answer "SELECT Id, FullName, Email, SecondaryEmail,
                // ImageUrl, CreatedAt, UpdatedAt WHERE Id = @id" entirely from this
                // slim index without touching the main clustered-index pages.
                b.HasIndex(u => u.Id)
                 .HasDatabaseName("IX_tbl_Users_Id_Profile")
                 .IncludeProperties(u => new
                 {
                     u.FullName,
                     u.Email,
                     u.SecondaryEmail,
                     u.ImageUrl,
                     u.CreatedAt,
                     u.UpdatedAt
                 });
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
