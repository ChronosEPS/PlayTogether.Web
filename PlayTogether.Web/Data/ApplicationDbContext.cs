using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlayTogether.Web.Models;

namespace PlayTogether.Web.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<GameMatch> Matches => Set<GameMatch>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<MatchPlayerRole> MatchPlayerRoles => Set<MatchPlayerRole>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Match>()
            .HasOne(m => m.Organizer)
            .WithMany(u => u.OrganizedMatches)
            .HasForeignKey(m => m.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MatchPlayer>()
            .HasIndex(mp => new { mp.MatchId, mp.UserId })
            .IsUnique();

        builder.Entity<MatchPlayerRole>()
            .HasOne(r => r.MatchPlayer)
            .WithMany(mp => mp.Roles)
            .HasForeignKey(r => r.MatchPlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
