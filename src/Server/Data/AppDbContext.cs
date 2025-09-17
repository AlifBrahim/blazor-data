using System.ComponentModel.DataAnnotations;
using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Server.Data;

public sealed class ApplicationUser : IdentityUser
{
    [StringLength(256)]
    public string? DisplayName { get; set; }
}

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProductEntry> ProductEntries => Set<ProductEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProductEntry>(entity =>
        {
            entity.ToTable("ProductEntries");
            entity.HasIndex(e => e.PartNumber);
            entity.HasIndex(e => e.CapturedAt);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UserId).IsRequired();
        });
    }
}
