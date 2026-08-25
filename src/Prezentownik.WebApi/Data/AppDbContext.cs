using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Models;

namespace Prezentownik.WebApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<GiftList> GiftLists => Set<GiftList>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<GiftClaim> GiftClaims => Set<GiftClaim>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<AppUser>(appUser =>
        {
            appUser.Property(au => au.DisplayName)
                .HasMaxLength(64);
        });

        builder.Entity<GiftList>(giftList =>
        {
            giftList.ToTable("GiftLists", schema: "app");

            giftList.HasKey(gl => gl.Id);
            giftList.Property(gl => gl.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();

            giftList.Property(gl => gl.Name)
                .HasMaxLength(128)
                .IsRequired();

            giftList.Property(gl => gl.Description)
                .HasMaxLength(1024);

            giftList.Property(gl => gl.OwnerId)
                .HasMaxLength(64);

            giftList.HasOne(gl => gl.Owner)
                .WithMany()
                .HasForeignKey(gl => gl.OwnerId);

            giftList.Property(gl => gl.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            giftList.Property(gl => gl.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            giftList.HasMany(gl => gl.Items)
                .WithOne(i => i.GiftList)
                .HasForeignKey(i => i.GiftListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Item>(item =>
        {
            item.ToTable("Items", schema: "app");

            item.HasKey(i => i.Id);
            item.Property(i => i.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();

            item.Property(i => i.Name)
                .HasMaxLength(128)
                .IsRequired();

            item.Property(i => i.Description)
                .HasMaxLength(1024);

            item.Property(i => i.Type)
                .IsRequired();

            item.Property(i => i.TargetQuantity);

            item.HasMany(i => i.Claims)
                .WithOne(c => c.Item)
                .HasForeignKey(c => c.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            item.Ignore(i => i.TotalClaimsQuantity);

            item.Property(i => i.OrderNumber)
                .IsRequired();
        });

        builder.Entity<GiftClaim>(claims =>
        {
            claims.ToTable("Claims", schema: "app");

            claims.HasKey(c => c.Id);
            claims.Property(c => c.Id)
                .HasValueGenerator<UuidV7ValueGenerator>();

            claims.Property(c => c.QuantityClaimed)
                .IsRequired();

            claims.Property(gl => gl.ClaimerId)
                .HasMaxLength(64);

            claims.HasOne(gl => gl.Claimer)
                .WithMany()
                .HasForeignKey(gl => gl.ClaimerId);

            claims.Property(c => c.ClaimerName)
                .HasMaxLength(64);

            claims.Property(c => c.RevocationToken);
        });
    }
}
