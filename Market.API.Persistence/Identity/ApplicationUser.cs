using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.API.Persistence.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        // Use version-7 GUIDs (time-ordered) so new rows are inserted at the end
        // of the clustered PK index instead of at a random position, eliminating
        // page splits and the fragmentation that caused slow FindByIdAsync lookups.
        public ApplicationUser() => Id = Guid.CreateVersion7();

        public string FullName { get; set; } = default!;
        public string? SecondaryEmail { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
