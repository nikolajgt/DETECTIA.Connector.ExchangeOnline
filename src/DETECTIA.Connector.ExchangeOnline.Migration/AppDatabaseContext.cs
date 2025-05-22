using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Migration;

public class AppDatabaseContext(DbContextOptions<AppDatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }  
    public DbSet<UserMailFolder> MailFolders { get; init; } 
    public DbSet<UserMailboxSettings> MailboxSettings { get; init; }
    public DbSet<ExchangeSyncState> SyncStates { get; init; }
    public DbSet<UserMessage> Messages { get; init; }
    public DbSet<UserMessageAttachment> MessagesAttachements { get; init; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("DETECTIA");
        modelBuilder.Entity<UserMailboxSettings>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
            b.HasOne<User>()
                .WithOne(u => u.UserMailboxSettings!)
                .HasForeignKey<UserMailboxSettings>(e => e.UserId);
        });
        
        modelBuilder.Entity<ExchangeSyncState>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        // modelBuilder.Entity<UserMessage>(b =>
        // {
        //     b.HasKey(e => e.Id);
        //     b.Property(e => e.Id).ValueGeneratedOnAdd();
        //     b.HasOne<UserMailFolder>()
        //         .WithOne(u => u.Id!)
        //         .HasForeignKey<UserMessage>(e => e.ExchangeUserId);
        // });
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