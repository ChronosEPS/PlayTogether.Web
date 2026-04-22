using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Models;

namespace PlayTogether.Web.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<MatchPlayerRole> MatchPlayerRoles => Set<MatchPlayerRole>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 👇 ДОБАВЬ ВОТ ЭТО
        builder.Entity<AppUser>()
            .Property(x => x.Balance)
            .HasPrecision(18, 2);

        builder.Entity<Match>()
            .Property(x => x.EntryFee)
            .HasPrecision(18, 2);

        builder.Entity<Match>()
            .HasOne(m => m.Organizer)
            .WithMany(u => u.OrganizedMatches)
            .HasForeignKey(m => m.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MatchPlayer>()
            .HasOne(mp => mp.Match)
            .WithMany(m => m.MatchPlayers)
            .HasForeignKey(mp => mp.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MatchPlayer>()
            .HasOne(mp => mp.User)
            .WithMany()
            .HasForeignKey(mp => mp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}