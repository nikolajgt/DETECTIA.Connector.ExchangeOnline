using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Migration;

public class AppDatabaseContext(DbContextOptions<AppDatabaseContext> options) : DbContext(options)
{
    public DbSet<ExchangeUser> Users { get; init; }  
    public DbSet<ExchangeMailFolder> MailFolders { get; init; } 
    public DbSet<UserMailboxSettings> MailboxSettings { get; init; }
    public DbSet<ExchangeUsersSyncState> SyncStates { get; init; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("DETECTIA");
    }
    
    public override int SaveChanges()
    {
        UpdateLastModified();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateLastModified();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateLastModified()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is IEntityTracker trackableEntity)
            {
                trackableEntity.LastSyncUtc = DateTimeOffset.UtcNow;
            }
        }
    }
}